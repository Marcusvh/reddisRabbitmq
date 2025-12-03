let playerId = "";

function playerName() {
    const playerLabel = document.getElementById("playerName");
    const inputTag = document.createElement("input");
    const playerUUID = crypto.randomUUID();
    inputTag.value = playerUUID;
    inputTag.disabled = true
    playerId = playerUUID;
    playerLabel.appendChild(inputTag);
}
playerName();

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/gamehub?playerId=" + playerId)
    .configureLogging(signalR.LogLevel.Information)
    .build();

const lobbyList = document.getElementById("lobbyList");
const grid = document.getElementById("grid");

// Initialize 5x5 grid
const cells = [];
for(let y=0; y<5; y++){
    for(let x=0; x<5; x++){
        const div = document.createElement("div");
        div.className = "cell";
        div.dataset.x = x;
        div.dataset.y = y;
        grid.appendChild(div);
        cells.push(div);
    }
}

// Lobby updates
connection.on("LobbyUpdate", evt => {
    const { playerId, action } = evt;
    if(action === "joined"){
        const li = document.createElement("li");
        li.id = playerId;
        li.innerText = playerId;
        lobbyList.appendChild(li);
    } else if(action === "left"){
        const li = document.getElementById(playerId);
        if(li) li.remove();
    }
});

// Match updates (turns / positions)
connection.on("TurnUpdate", evt => {
    const { playerStates } = evt; // { playerId: {x, y, hp} }
    cells.forEach(c => c.innerText = "");
    for(const [pid, state] of Object.entries(playerStates)){
        const cell = cells.find(c => c.dataset.x==state.x && c.dataset.y==state.y);
        if(cell) cell.innerText = pid + "(" + state.hp + ")";
    }
});

connection.start().then(() => console.log("Connected"));

// Lobby buttons
document.getElementById("joinLobby").addEventListener("click", async () => {
    await connection.invoke("JoinLobby", playerId);
});
document.getElementById("leaveLobby").addEventListener("click", async () => {
    await connection.invoke("LeaveLobby", playerId);
});

// Movement buttons
async function sendMove(dir){
    let ehh = await connection.invoke("SendAction", "demo-match", { PlayerId: playerId, Type: "Move", Direction: dir });
    console.log(ehh);
    
}
document.getElementById("moveN").onclick = () => sendMove("N");
document.getElementById("moveS").onclick = () => sendMove("S");
document.getElementById("moveE").onclick = () => sendMove("E");
document.getElementById("moveW").onclick = () => sendMove("W");
document.getElementById("attack").onclick = async () => await connection.invoke("SendAction", "demo-match", { PlayerId: playerId, Type: "Attack" });
