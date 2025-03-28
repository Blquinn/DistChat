<script lang="ts">
    import * as signalr from "@microsoft/signalr";
	import { onMount } from "svelte";

    let username= $state<string>("");
    let message = $state<string>("");
    let hostUrls = $state<string[]>([]);
    let connection: signalr.HubConnection | null = $state(null);
    let connectionState: string = $state("uninitialized");
    let receivedMessages: string[] = $state([]);
    
    async function connectToHost(host: string): Promise<void> {
        if (connection) {
            await connection.stop();
            connection = null;
            connectionState = "uninitialized";
        }
        
        connection = new signalr.HubConnectionBuilder()
            .withUrl(`${host}/chat`)
            .withAutomaticReconnect()
            // .withServerTimeout(10000)
            .withKeepAliveInterval(30000)
            .build();
        
        connection.on("ReceiveMessage", (user: string, message: string) => {
            receivedMessages.push(`${user} -- ${message}`);
        });
        
        connection.onclose(() => {
            connectionState = "closed";
        });
        connection.onreconnecting(() => {
            connectionState = "reconnecting";
        });
        connection.onreconnected(() => {
            connectionState = "connected";
        });
        
        await connection.start();
        connectionState = "connected";
    }
    
    async function sendMessage() {
        if (!connection) return;
        
        await connection.send("SendToRoom", "messages", username, message);
    }
    
    onMount(async () => {
        const res = await fetch("http://localhost:5043/cluster/list-hosts");
        hostUrls = await res.json();
    });
</script>

<div>
    <p>{connectionState}</p>
</div>

<ul>
    {#each hostUrls as host}
        <li>
            <button onclick={() => connectToHost(host)}>Connect to {host}</button>
        </li>
    {/each}
</ul>

{#if connection}
    <div>
        <input type="text" placeholder="Username" bind:value={username}>
        <input type="text" placeholder="Message" bind:value={message}>
        <button onclick={sendMessage}>Send</button>
    </div>
{/if}

<ul>
    {#each receivedMessages as message}
        <li>{message}</li>
    {/each}
</ul>
