const url = '/api';
var displayDate = new Date();

async function loadDevices(isBackground) {
    const container = document.getElementById('live-data');

    if (!isBackground) {
        container.innerHTML = '<p class="loading">Loading devices...</p>';
    }

    try {
        const response = await fetch(`${url}/Devices`);

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const devices = await response.json();                

        if (devices.length === 0) {
            container.innerHTML = '<p>No devices found.</p>';
            return;
        }

        let html = '';

        devices.forEach(device => {
            html += `
                <div id=${device.deviceId} class="col-6">
                    <div class="card text-center mt-3">
                        <div class="card-body">
                            <p class="card-title truncate">${device.zoneName}</p>
                            <p class="card-text">`;

            if (device.isThermostat) {
                html += `
                                <span class="${device.isHeating ? "on" : device.isPreheating ? "pre" : ""}" style="font-size:2em;">
                                    ${device.actualTemp}&deg;
                                </span>`;
            }
            else {
                html += `
                                <span class="${device.timerOn ? "on" : ""}" style="font-size:2em;">
                                ${device.timerOn ? "ON" : "OFF"}
                                </span>`;
            }

            html += `
                            </p>
                            <p class="card-subtitle mb-2 text-muted">`;

            if (device.isThermostat) {
                html += `
                                <button class="btn btn-light btn-sm rounded-circle p-2 lh-1" onclick="setTemp('${device.zoneName}', ${device.setTemp - 0.5})">
                                    <i class="fa fa-minus"></i></button>
                                ${device.setTemp}&deg;
                                <button class="btn btn-light btn-sm rounded-circle p-2 lh-1" onclick="setTemp('${device.zoneName}', ${device.setTemp + 0.5})">
                                    <i class="fa fa-plus"></i></button>`;
            }
            else {
                html += `
                                <button class="btn btn-light btn-sm p-2 lh-1" type="button">Boost 1hr</button>`;
            }
            html += `               
                            </p>
                        </div>
                    </div>
                </div>
            `;
        });

        container.innerHTML = html;

    } catch (error) {
        if (!isBackground) {
            container.innerHTML = `<p class="error">Error loading devices: ${error.message}</p>`;
        }
        console.error('Error:', error);
    }
}

async function loadDevicesFull() {            
    const container = document.getElementById('live-data');

    container.innerHTML = '<p class="loading">Loading devices...</p>';            

    try {
        const response = await fetch(`${url}/Devices?includeAdvancedData=true`);

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const devices = await response.json();                

        if (devices.length === 0) {
            container.innerHTML = '<p>No devices found.</p>';
            return;
        }

        let tableHtml = `
            <table class="table table-striped">
                <thead>
                    <tr>
                        <th>Name</th>
                        <th>Current Profile</th>
                        <th>Current Temp</th>
                        <th>Set Temp</th>
                        <th>RoC</th>
                        <th>Max Preheat</th>
                    </tr>            
                </thead>
                <tbody>`;

        devices.forEach(device => {

            if (device.isThermostat) {

                tableHtml += `
                    <tr id="d${device.deviceId}" class="${device.isHeating ? "on" : device.isPreheating ? "pre" : ""}">
                        <th>${device.zoneName}</th>
                        <td>${device.profileName}</td>
                        <td>${device.actualTemp}&deg;</td>
                        <td>${device.setTemp}&deg;</td>
                        <td>${device.roC}</td>
                        <td>${device.maxPreheatHours} hours</td>
                    </tr>`;
            }
        });

        tableHtml += `
                </tbody>
            </table>`;

        container.innerHTML = tableHtml;

    } catch (error) {
        container.innerHTML = `<p class="error">Error loading devices: ${error.message}</p>`;
        console.error('Error:', error);
    }
}

async function loadHistory() {    
    const tableContainer = document.getElementById('tableContainer');
    
    const displayDateDiv = document.getElementById('displayDate');
    displayDateDiv.textContent = displayDate.toLocaleDateString('en-GB', { weekday: 'short', year: 'numeric', month: 'short', day: 'numeric' });
                
    tableContainer.innerHTML = '<p class="loading">Loading history data...</p>';    

    try {        
        const response = await fetch(`${url}/devices/history?date=${displayDate.toISOString().split('T')[0]}`);

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const historyData = await response.json();

        if (historyData.length === 0) {
            tableContainer.innerHTML = '<p>No history data found for this date.</p>';
            return;
        }

        let tableHtml = `
            <div id="heating-table">
            <table class="table">
                <tbody>                            
        `;
        

        historyData.forEach(device => {
            tableHtml += `
                <tr>
                    <th class="room">${device.deviceName}</th>
            `;

            device.history.forEach((value, i) => {
                let heatClass = '';
                let lineClass = (i)%4==0 ? 'line' : '';
                let title = 'No data';

                switch (value) {
                    case 0:
                        heatClass = 'off';
                        title = 'Off';
                        break;
                    case 1:
                        heatClass = 'on';
                        title = 'Heating';
                        break;
                    case 2:
                        heatClass = 'pre';
                        title = 'Preheating';
                        break;
                    case -1:
                    default:
                        heatClass = '';
                        title = 'No data';
                }

                tableHtml += `<td class="heat ${heatClass} ${lineClass}" title="${title}">&nbsp;</td>`;
            });

            tableHtml += `                            
                </tr>
            `;
        });

        tableHtml += `
                <tr class="timeline">
                    <th class="room">&nbsp;</th>                        
                    <th colspan="2"></th>
        `;

        // Create hour markers
        for (let i = 1; i < 24; i++) {
            tableHtml += `<th colspan="4" style="text-align:center">${Number(i.toPrecision(2))}:00</th>`;                    
        }

        tableHtml += `
                    <th colspan="2">&nbsp;</th>
                    </tr>
                </tbody>
            </table>
            </div>
        `;

        tableContainer.innerHTML = tableHtml;

    } catch (error) {
        tableContainer.innerHTML = `<p class="error">Error loading history: ${error.message}</p>`;
        console.error('Error:', error);
    }
}

function incrDate() {
    displayDate.setDate(displayDate.getDate() + 1);            
    loadHistory();
}

function decrDate() {
    displayDate.setDate(displayDate.getDate() - 1);            
    loadHistory();
}

function toggleMenu() {
    var x = document.getElementById("nav-items");
    if (x.style.display === "block") {
        x.style.display = "none";
    } else {
        x.style.display = "block";
    }
}

async function loadSchedules() {

    const schedulesContainer = document.getElementById('schedules');
    schedulesContainer.innerHTML = '<p class="loading">Loading schedules...</p>';    

    try {
        const response = await fetch(`${url}/Schedules`);

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const schedules = await response.json();        

        if (schedules.length === 0) {
            schedulesContainer.innerHTML = '<p>No schedules found.</p>';
            return;
        }

        let scheduleHtml = `
            <table class="table table-striped" width="100%">
            <thead>
                <tr>
                    <th></th>
                    <th colspan="4" style="text-align:center">Weekdays</th>
                    <th colspan="4" style="text-align:center">Weekends</th>
                </tr>
                <tr>
                    <th></th>
                    <th style="text-align:center">Wake</th>
                    <th style="text-align:center">Leave</th>
                    <th style="text-align:center">Return</th>
                    <th style="text-align:center">Sleep</th>
                    <th style="text-align:center">Wake</th>
                    <th style="text-align:center">Leave</th>
                    <th style="text-align:center">Return</th>
                    <th style="text-align:center">Sleep</th>
                </tr>
            </thead>
            <tbody>`;


        schedules.forEach(item => {
            scheduleHtml += `
                    <tr>
                        <th>${item.scheduleName}</th>`;

            item.intervals.forEach(i => {
                scheduleHtml += `<td width="11%" style="text-align:center">${i.time.slice(0, 5)}<br /><span style="font-weight:bold;font-size:1.2em;">${i.targetTemp}</span>&deg;</td>`;
            })

            scheduleHtml += `
                    </tr>`;

        });    

        scheduleHtml += `
            </tbody>
        </table>`;
        
        schedulesContainer.innerHTML = scheduleHtml;

    } catch (error) {
        schedulesContainer.innerHTML = `<p class="error">Error loading schedules: ${error.message}</p>`;
        console.error('Error:', error);
    }
}

async function loadAutomations() {

    const container = document.getElementById('automations');
    container.innerHTML = '<p class="loading">Loading automations...</p>';

    try {
        const response = await fetch(`${url}/Actions`);

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const automations = await response.json();

        if (automations.length === 0) {
            container.innerHTML = '<p>No automations found.</p>';
            return;
        }

        let automationsHtml = '';

        automations.forEach(item => {
            automationsHtml += `
                <div class="col-md-6">
                    <div class="card text-center mt-3">
                        <div class="card-body">
                            <h5 class="card-title truncate">${item.name}</h5>
                            <p class="card-text">${item.description}</p>
                            <p class="card-text">Schedule: ${item.schedule}</p>
                            <button id="${item.id}Btn" class="btn btn-primary mr-2" onclick="runAction('${item.id}');"><i class="fa fa-bolt">&nbsp;</i> Run Now</button>
                        </div>
                    </div>
                </div>`;            
        });        

        container.innerHTML = automationsHtml;

    } catch (error) {
        container.innerHTML = `<p class="error">Error loading schedules: ${error.message}</p>`;
        console.error('Error:', error);
    }
}

async function loadNav() {
    const navContainer = document.getElementById('nav-container');
    navContainer.innerHTML= `
    <div class="topnav">
        <a href="javascript:void(0);" class="banner" onclick="toggleMenu()">NeoConnect</a>
        <div id="nav-items">
            <div class="nav-item px-3"><a class="navlink" href="index.html"><span class="fa fa-bolt"></span>Live Data</a></div>
            <div class="nav-item px-3"><a class="navlink" href="automations.html"><span class="fa fa-magic"></span>Automations</a></div>
            <div class="nav-item px-3"><a class="navlink" href="devices.html"><span class="fa fa-thermometer-half"></span>Devices</a></div>
            <div class="nav-item px-3"><a class="navlink" href="schedules.html"><span class="fa fa-calendar-o"></span>Schedules</a></div>
            <div class="nav-item px-3"><a class="navlink" href="history.html"><span class="fa fa-line-chart"></span>History</a></div>
            <div class="nav-item px-3"><a class="navlink" href="logs.html"><span class="fa fa-file-text-o"></span>Logs</a></div>
        </div>
        <a href="javascript:void(0);" class="bars" onclick="toggleMenu()">
            <i class="fa fa-bars"></i>
        </a>
    </div>`;
}

async function runAction(actionName) {
    const btn = document.getElementById(actionName + 'Btn');
    btn.disabled = true;
    const response = await fetch(`${url}/Actions?actionName=${actionName}`, {method: "POST"});    
    btn.disabled = false;

    if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
    }
}

async function setTemp(deviceName, temp) {    
    let btns = document.getElementsByClassName('btn');
    for (let i = 0; i < btns.length; i++) {
        btns[i].disabled = true;
    }

    const response = await fetch(`${url}/Devices/temperature`, {
        method: "PUT",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({ zoneName: deviceName, setTemp: temp }),
    });

    for (let i = 0; i < btns.length; i++) {
        btns[i].disabled = false;
    }

    if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
    }

    //reload
    await loadDevices(true);
}

async function loadLogs() {
    const logsContainer = document.getElementById('logs-container');
    logsContainer.innerHTML = '<p class="loading">Loading logs...</p>';

    try {
        const logLevelFilter = document.getElementById('logLevelFilter');
        const logCountFilter = document.getElementById('logCountFilter');
        const logSortOrder = document.getElementById('logSortOrder');
        const level = logLevelFilter ? logLevelFilter.value : '';
        const count = logCountFilter ? logCountFilter.value : '50';
        const sortOrder = logSortOrder ? logSortOrder.value : 'asc';

        let fetchUrl = `${url}/Logs?count=${count}`;
        if (level) {
            fetchUrl += `&level=${level}`;
        }

        const response = await fetch(fetchUrl);

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        let logs = await response.json();

        if (logs.length === 0) {
            logsContainer.innerHTML = '<p>No logs found.</p>';
            return;
        }

        logs.sort((a, b) => {
            const dateA = new Date(a.timestamp);
            const dateB = new Date(b.timestamp);
            return sortOrder === 'asc' ? dateA - dateB : dateB - dateA;
        });

        let tableHtml = `
            <div style="max-height:75vh;overflow-y:scroll">
            <table class="table table-striped">                
                <tbody>`;

        logs.forEach(log => {
            let logLevelClass = '';
            switch(log.logLevel.toLowerCase()) {
                case 'error':
                    logLevelClass = 'table-danger';
                    break;
                case 'warning':
                    logLevelClass = 'table-warning';
                    break;
                case 'information':
                    logLevelClass = '';
                    break;
                case 'debug':
                    logLevelClass = 'table-info';
                    break;
            }

            const timestamp = new Date(log.timestamp).toLocaleString('en-GB');

            tableHtml += `
                <tr class="${logLevelClass}">
                    <td>${timestamp}</td>                    
                    <td>
                        ${log.message}
                        ${log.exception ? `<br/><small class="text-danger">Exception: ${log.exception}</small>` : ''}
                    </td>
                    <td>${log.category}</td>
                </tr>`;
        });

        tableHtml += `
                </tbody>
            </table>
            </div>`;

        logsContainer.innerHTML = tableHtml;

    } catch (error) {
        logsContainer.innerHTML = `<p class="error">Error loading logs: ${error.message}</p>`;
        console.error('Error:', error);
    }
}

async function clearLogs() {
    const btn = document.getElementById('clearLogsBtn');

    if (!confirm('Are you sure you want to clear all logs?')) {
        return;
    }

    btn.disabled = true;

    try {
        const response = await fetch(`${url}/Logs`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        await loadLogs();

    } catch (error) {
        alert(`Error clearing logs: ${error.message}`);
        console.error('Error:', error);
    } finally {
        btn.disabled = false;
    }
}