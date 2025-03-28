"use strict";

document.addEventListener("DOMContentLoaded", async () => {
    let connection = new signalR.HubConnectionBuilder()
        .withUrl("/chat")
        .build();
    
    document.getElementById("sendButton").disabled = true;

    connection.on("ReceiveMessage", (user, message) => {
        let li = document.createElement("li");
        li.textContent = `${user} says ${message}`;
        document.getElementById("messagesList").appendChild(li);
    });
    
    try {
        await connection.start();
        document.getElementById("sendButton").disabled = false;
    } catch (error) {
        console.error(error.toString());
    }
    
    // setTimeout(async () => {
    //     await connection.stop();
    // }, 3000)

    document.getElementById("sendButton").addEventListener("click", async (event) => {
        let user = document.getElementById("userInput").value;
        let message = document.getElementById("messageInput").value;
        event.preventDefault();
        try {
            await connection.invoke("SendMessage", user, message);
        } catch (error) {
            console.error(error.toString());
        }
    });
})
