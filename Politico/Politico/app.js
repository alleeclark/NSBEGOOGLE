var app = angular.module('blackTweetsApp', [
    'ngRoute', 
    'ngMaterial',
    "firebase",
])


//Making the checkboxes Burgundy in AngularMaterial
.config(function($mdThemingProvider) {
    var burgundyButtonColor = $mdThemingProvider.extendPalette('amber', {
        '900': 'ffcc00'
    });
    $mdThemingProvider.definePalette('amber', burgundyButtonColor);
    $mdThemingProvider.theme('default')
    .primaryPalette('amber', {
        'default': '900'
    })
    .accentPalette('amber', {
        'default': '900'
    });
});


//Controls initial landing page
app.controller('mainController', function($scope, $http, $location) {
    $scope.changeview = function(path) {
        $location.path(path);
    }


     // List data and initial item
  $scope.candidates = {
    HC: "Hillary Clinton",
    BS: "Bernie Sanders",
    TC: "Ted Cruz",
    DT: "Donald Trump",
    BC: "Ben Carson"
    }
  
  $scope.hashtags = {
    BLM: "#BlackLivesMatter",
    SB: "#SandraBland",
    YAW: "#YesAllWomen",
    OSW: "#OscarsSoWhite",
    MSF: "#MasculinitySoFragile"
    }

  $scope.time = {
    TH: "this hour",
    TD: "today",
    TW: "this week",
    TM: "this month",
    TY: "this year"
    }
  

  //Current Item Variables
  $scope.currentItemCandidates = $scope.candidates.BS;
  $scope.currentHashtags = $scope.hashtags.SB;
  $scope.currentTime = $scope.time.TD;


  // Field Open Variables
  $scope.candidateOpen = false;
  $scope.tagOpen = false;
  $scope.timeOpen = false;

  //Control Candidates Field
  $scope.toggleCandidateOpen = function( key ){
    if ( key ) {
      $scope.currentItemCandidates = $scope.candidates[key];
    }
    $scope.candidateOpen = !$scope.candidateOpen;
  };
  

  //Control Hashtags Field
  $scope.toggleTagOpen = function( key ){
    if ( key ) {
      $scope.currentHashtags = $scope.hashtags[key];
    }
    $scope.tagOpen = !$scope.tagOpen;
  };

  //Control Hashtags Field
  $scope.toggleTime = function( key ){
    if ( key ) {
      $scope.currentTime = $scope.time[key];
    }
    $scope.timeOpen = !$scope.timeOpen;
  };
});

//Switches between pages/views
app.config(['$routeProvider', '$locationProvider', function($routeProvider, $locationProvider) {
    $routeProvider.
    when('/', {
        templateUrl: 'main.html',
    }).
    when('/Petitions', {
        templateUrl: 'petitions.html',
    }).
    when('/Events', {
        templateUrl: 'events.html',
    }).
    when('/Discussions', {
        templateUrl: 'discussions.html',
    }).
    when('/PendingActions', {
        templateUrl: 'pending-actions.html',
    }).
    when('/AllDiscussions', {
        templateUrl: 'all-discussions.html',
    }).
    otherwise({
        redirectTo: '/'
    });
}]);


exports = app;