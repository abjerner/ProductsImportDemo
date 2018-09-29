angular.module("umbraco").controller("Products.ProductData.Controller", function($scope) {

    $scope.data = null;

    if ($scope.model.value) {
        $scope.data = $scope.model.valueJson = JSON.parse($scope.model.value.substr(1));
        $scope.dataSize = $scope.model.value.length / 1024;
    }

    if (!$scope.data) return;

});