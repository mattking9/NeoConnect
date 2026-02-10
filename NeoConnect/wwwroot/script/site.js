const url = '/api';
var displayDate = new Date();

async function loadDevices() {            
    const container = document.getElementById('live-data');

    container.innerHTML = '<p class="loading">Loading devices...</p>';            

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
                            <p class="card-text">
                                <span class="${(device.isHeating || device.timerOn) ? "on" : device.isPreheating ? "pre" : ""}" style="font-size:2em;">
                                    ${(device.isThermostat ? device.actualTemp + "&deg;" : device.timerOn ? "ON" : "OFF")}
                                </span>
                            </p>
                            <p class="card-subtitle mb-2 text-muted">${(device.isThermostat ? device.setTemp + "&deg;" : "n/a")}</p>
                        </div>
                    </div>
                </div>
            `;
        });

        container.innerHTML = html;

    } catch (error) {
        container.innerHTML = `<p class="error">Error loading devices: ${error.message}</p>`;
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
                    <tr id="d${device.deviceId}">
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
        const response = await fetch(`${url}/History?date=${displayDate.toISOString().split('T')[0]}`);

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
        </div>
        <a href="javascript:void(0);" class="bars" onclick="toggleMenu()">
            <i class="fa fa-bars"></i>
        </a>
    </div>`;
}

async function dataCollection() {
    runAction("data_collection", "dataCollectionBtn")
}

async function runAction(actionName, btnId) {
    const btn = document.getElementById(btnId);
    btn.disabled = true;
    const response = await fetch(`${url}/Actions?actionName=${actionName}`, {method: "POST"});

    if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
    }

    btn.disabled = false;
}