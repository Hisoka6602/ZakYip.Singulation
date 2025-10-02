/*import * as signalR from "@microsoft/signalr"

const conn = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/events")
    .withAutomaticReconnect([0, 1000, 2000, 5000, 10000])
    .build()

conn.on("event", (envelope: any) => {
    const { channel, type, data, ts, seq } = envelope
    // TODO: 根据 channel 分流到不同 reducer/store
    console.log(channel, type, seq, data)
})

await conn.start()
// 订阅频道
await conn.invoke("Join", "/vision/speed.decoded")
await conn.invoke("Join", "/device")
// 如需要原始 HEX：
await conn.invoke("Join", "/vision/speed.raw")

// 离开频道
// await conn.invoke("Leave", "/vision/speed.raw");*/