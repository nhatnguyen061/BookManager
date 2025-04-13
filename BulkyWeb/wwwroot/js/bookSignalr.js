
"use strict";
var connectionNotification = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/book").build();

connectionNotification.on("ReceiveMessage", function (message) {
    console.log("ReceiveMessage received! Reloading page...");
    window.location.reload(); // Trực tiếp tải lại trang
});
function fulfilled() {
    //do something on start
    console.log("Connection to User Hub Successful");
    

}
function rejected() {
    //rejected logs
    console.log(DOMException.toString());
    console.log("reject");
}
connectionNotification.start().then(fulfilled, rejected);