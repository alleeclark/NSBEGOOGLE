﻿using Microsoft.HBase.Client;
using org.apache.hadoop.hbase.rest.protobuf.generated;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tweetinvi.Core.Interfaces;

namespace HBaseWriter
{
    public class HBaseWriter
    {
        HBaseClient client;

        const string TABLE_BY_WORDS_NAME = "tweets_by_words";
        const string COUNT_ROW_KEY = "~ROWCOUNT";
        const string COUNT_COLUMN_NAME = "d:COUNT";

        long rowCount = 0;

        IDictionary<string, DictionaryItem> dictionary;

        Thread writerThread;
        Queue<ITweet> queue = new Queue<ITweet>();
        bool threadRunning = true;

        public HBaseWriter()
        {
            var credenitals = CreateFromFile(@"..\..\credentials.txt");
            client = new HBaseClient(credenitals);
            TableList tables = client.ListTablesAsync().Result;

            //if (!client.ListTablesAsync().name.Contains(TABLE_BY_WORDS_NAME))
            if(!tables.name.Contains(TABLE_BY_WORDS_NAME))    
                {
                var tableSchema = new TableSchema();
                tableSchema.name = TABLE_BY_WORDS_NAME;
                tableSchema.columns.Add(new ColumnSchema { name = "d" });
                client.CreateTableAsync(tableSchema);// CreateTable(tableSchema);
                Console.WriteLine("Table \"{0}\" created.", TABLE_BY_WORDS_NAME);

            }

            rowCount = GetRowCount();

            LoadDictionary();

            writerThread = new Thread(new ThreadStart(WriteThreadFunction));
            writerThread.Start();
        }

        ~HBaseWriter()
        {
            threadRunning = false;
        }

        private long GetRowCount()
        {
            try
            {
                var cellSet = client.GetCellsAsync(TABLE_BY_WORDS_NAME, COUNT_ROW_KEY).Result;

                if (cellSet.rows.Count != 0)
                {
                    var countCol = cellSet.rows[0].values.Find(cell => Encoding.UTF8.GetString(cell.column) == COUNT_COLUMN_NAME);
                    if (countCol != null)
                    {
                        return Convert.ToInt64(Encoding.UTF8.GetString(countCol.data));
                    }
                }
            }
            catch (Exception ex)
            {
                return 0;
            }
            return 0;
        }

        private void CreateRowCountCell(CellSet set, long count)
        {
            var row = new CellSet.Row { key = Encoding.UTF8.GetBytes(COUNT_ROW_KEY) };

            var value = new Cell
            {
                column = Encoding.UTF8.GetBytes(COUNT_COLUMN_NAME),
                data = Encoding.UTF8.GetBytes(count.ToString())

            };
            row.values.Add(value);
            set.rows.Add(row);
        }

        public void WriteTweet(Tweetinvi.Core.Interfaces.ITweet tweet)
        {
            lock (queue)
            {
                queue.Enqueue(tweet);
            }
        }

        public void WriteThreadFunction()
        {
            while (threadRunning)
            {
                try
                {
                    if (queue.Count > 0)
                    {
                        var set = new CellSet();
                        lock (queue)
                        {
                            do
                            {
                                var tweet = queue.Dequeue();

                                CreateTweetByWordCells(set, tweet);
                            } while (queue.Count > 0);
                        }
                        CreateRowCountCell(set, rowCount + set.rows.Count);
                        client.StoreCellsAsync(TABLE_BY_WORDS_NAME, set);
                        //client.StoreCells(TABLE_BY_WORDS_NAME, set);

                        rowCount += set.rows.Count;

                        Console.WriteLine("==={0} rows written ====", set.rows.Count);
                    }
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: " + ex.Message + "\nStackTrace \n" + ex.StackTrace);
                }
            }
        }

        private static char[] _punctuationChars = new[] {
             ' ', '!', '\"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/',   //ascii 23--47 
             ':', ';', '<', '=', '>', '?', '@', '[', ']', '^', '_', '`', '{', '|', '}', '~' };

        private void CreateTweetByWordCells(CellSet set, ITweet tweet)
        {
            var words = tweet.Text.ToLower().Split(_punctuationChars);
            int sentimentScorer = CalcSentimentScore(words);
            var word_pairs = words.Take(words.Length - 1) .Select((word, idx) => string.Format("{0} {1", words, words[idx + 1]));
            var all_words = words.Concat(word_pairs).ToList();

            foreach (var word in all_words)
            {
                var time_index = (ulong.MaxValue - (ulong)tweet.CreatedAt.ToBinary()).ToString().PadLeft(20) + tweet.IdStr;
                var key = word + "_" + time_index;
                var row = new CellSet.Row { key = Encoding.UTF8.GetBytes(key) };

                var value = new Cell { column = Encoding.UTF8.GetBytes("d:id_str"), data = Encoding.UTF8.GetBytes(tweet.IdStr) };
                row.values.Add(value);
            }

        }

        private int CalcSentimentScore(string[] words)
        {
            var total = 0;
            foreach (var word in words)
            {
                if (dictionary.Keys.Contains(word))
                {
                    switch (dictionary[word].Polarity)
                    {
                        case "negative": total -= 1; break;
                        case "positive": total += 1; break;
                    }
                }
            }
            if (total > 0)
            {
                return 1;
            }
            else if (total < 0)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
        public static void CreateTweetCells(CellSet set, ITweet tweet)
        {
            var key = tweet.IdStr;
            var row = new CellSet.Row { key = Encoding.UTF8.GetBytes(key) };
            var value = new Cell { column = Encoding.UTF8.GetBytes("d:created_at"), data = Encoding.UTF8.GetBytes(tweet.CreatedAt.ToLongTimeString()) };
            row.values.Add(value);
            value = new Cell { column = Encoding.UTF8.GetBytes("d:text"), data = Encoding.UTF8.GetBytes(tweet.Text) };
            row.values.Add(value);
            if (tweet.Coordinates != null)
            {
                var str = tweet.Coordinates.Longitude.ToString() + "," + tweet.Coordinates.Latitude.ToString();
                value = new Cell { column = Encoding.UTF8.GetBytes("d:coor"), data = Encoding.UTF8.GetBytes(str) };
                row.values.Add(value);
            }
            if (tweet.Place != null)
            {
                value = new Cell { column = Encoding.UTF8.GetBytes("d:place_fullname"), data = Encoding.UTF8.GetBytes(tweet.Place.FullName) };
                row.values.Add(value);
            }
            set.rows.Add(row);
        }

        private void LoadDictionary()
        {
            List<string> lines = File.ReadAllLines("").ToList();
            var items = lines.Select(line =>
            {
                var fields = line.Split('\t');
                var pos = 0;
                return new DictionaryItem
                {
                    Type = fields[pos++],
                    Length = Convert.ToInt32(fields[pos++]),
                    Word = fields[pos++],
                    Pos = fields[pos++],
                    Polarity = fields[pos++]


                };

            });
            dictionary = new Dictionary<string, DictionaryItem>();
            foreach (var item in items)
            {
                if (!dictionary.Keys.Contains(item.Word))
                {
                    dictionary.Add(item.Word, item);
                }
            }

        }
        internal static ClusterCredentials CreateFromFile(string path)
        {
            List<string> lines = File.ReadAllLines(path).ToList();
            return CreateFromList(lines);
        }
        internal static ClusterCredentials CreateFromList(List<string> lines)
        {
            if (lines.Count() != 3)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Expected," +
                        "first containing" + "Given {0} lines!",
                        lines.Count()),
                    "lines");
            }
            var rv = new ClusterCredentials(new Uri(lines[0]), lines[1], lines[2]);
            return rv;
        }
        public class DictionaryItem
        {
            public string Type { get; set; }
            public int Length { get; set; }
            public string Word { get; set; }
            public string Pos { get; set; }
            public string Stemmed { get; set; }
            public string Polarity { get; set; }
        }
    }
}
