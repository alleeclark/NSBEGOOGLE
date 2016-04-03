using Microsoft.HBase.Client;
using org.apache.hadoop.hbase.rest.protobuf.generated;
using System;
using System.Collections.Generic;
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

            if(!client.ListTables().name.Contains(TABLE_BY_WORDS_NAME))
            {
                var tableSchema = new TableSchema();
                tableSchema.name = TABLE_BY_WORDS_NAME;
                tableSchema.columns.Add(new ColumnSchema { name = "d" });
                client.CreateTable(tableSchema);
                Console.WriteLine("Table \"{0}\" created.", TABLE_BY_WORDS_NAME);
 
            }

            rowCount = GetRowCount();

            LoadDictionary();

            writerThread = new Thread(new ThreadStart(writerThreadFunction));
            writerThread.Start();
        }

        ~HBaseWriter()
        {
            threadRunning = false;
        }

        private long GetRowCount()
        {
            try {
                var cellSet = client.GetCells(TABLE_BY_WORDS_NAME, COUNT_ROW_KEY);
                if (cellSet.rows.Count != 0)
                {
                    var count = cellSet.rows[0].values.Find(cell => Encoding.UTF8.GetString(cell.colum) == COUNT_COLUMN_NAME);
                    if (countCol != null)
                    {
                        return Convert.ToInt64(Encoding.UTF8.GetString(countCol.data));
                    }
                }
            }
            catch(Exception ex)
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
            lock(queue)
            {
                queue.Enqueue(tweet);
            }
        }

        public WriteThreadFunction()
        {
            while(threadRunning)
            {
                try
                {
                    if(queue.Count > 0)
                    {
                        var set = new CellSet();
                        lock(queue)
                        {
                            do
                            {
                                var tweet = queue.Dequeue();

                                CreateTweetByWordsCells(set, tweet);
                            } while (queue.Count > 0);
                        }
                        CreateRowCountCell(set, rowCount + set.rows.Count);

                        client.StoreCells(TABLE_BY_WORDS_NAME, set);

                        rowCount += set.rows.Count;

                        Console.WriteLine("==={0} rows written ====", set.rows.Count);
                    }
                    Thread.Sleep(100);
                }
                catch(Exception ex)
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
            var word_pairs = words.Take(words.Length - 1) Select((words, idx) => string.Format("{0} {1", words, words[idx + 1]));
            var all_words = words.Concat(word_pairs).ToList();

            foreach(var word in all_words)
            {
                var time_index =(ulong.MaxValue - (ulong)tweet.CreatedAt.ToBinary()).ToString().PadLeft(20) + tweet.IdStr;
                var key = word + "_" + time_index;
                var row = new CellSet.Row { key = Encoding.UTF8.GetBytes(key) };

                var value = new Cell { column = Encoding.UTF8.GetBytes("d:id_str"), data = Encoding.UTF8.GetBytes(tweet.IdStr) };
                row.values.Add(value);
            }
            
        }

        private int CalcSentimentScore(string[] words)
        {

        }


        private object CreateFromFile(string v)
        {
            throw new NotImplementedException();
        }
    }
}
