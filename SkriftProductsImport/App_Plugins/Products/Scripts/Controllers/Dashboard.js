angular.module("umbraco").controller("Products.Dashboard.Controller", function($scope, $http) {

    $scope.result = null;

    $scope.import = function () {
        $scope.loading = true;
        $http.get("/umbraco/backoffice/api/Products/Import").success(function (r) {
            $scope.loading = false;
            $scope.result = r;
        });
    };

});