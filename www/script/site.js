async function loadDevices() {            
            const container = document.getElementById('live-data');

            container.innerHTML = '<p class="loading">Loading devices...</p>';            

            try {
                const response = await fetch('https://localhost:7024/Devices');

                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }

                const devices = await response.json();                

                if (devices.length === 0) {
                    container.innerHTML = '<p>No devices found.</p>';
                    return;
                }

                let tableHtml = '';

                devices.forEach(device => {
                    tableHtml += `
                        <div id=${device.deviceId} class="col-6">
                            <div class="card text-center mt-3">
                                <div class="card-body">
                                    <p class="card-title truncate">${device.zoneName}</p>
                                    <p class="card-text">
                                        <span class="${(device.isHeating || device.timerOn) ? "on" : device.isPreheating ? "pre" : ""}" style="font-size:2em;">
                                            ${(device.isThermostat ? device.actualTemp + "°" : device.timerOn ? "ON" : "OFF")}
                                        </span>
                                    </p>
                                    <p class="card-subtitle mb-2 text-muted">${(device.isThermostat ? device.setTemp + "°" : "n/a")}</p>
                                </div>
                            </div>
                        </div>
                    `;
                });

                container.innerHTML = tableHtml;

            } catch (error) {
                container.innerHTML = `<p class="error">Error loading devices: ${error.message}</p>`;
                console.error('Error:', error);
            }
        }

        function toggleMenu() {
            var x = document.getElementById("nav-items");
            if (x.style.display === "block") {
                x.style.display = "none";
            } else {
                x.style.display = "block";
            }
        }