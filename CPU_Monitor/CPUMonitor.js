define(["require", 'mainTabsManager', "jQuery", "globalize", "emby-checkbox", "emby-select", "emby-linkbutton", "emby-input", "emby-button"],
    function (require, mainTabsManager, $, globalize) {

        var pluginUniqueId = "EBAB59A3-8C08-4A27-852B-998319387B21";
        let cpuMonitor; // Declare globally so we can control it across events
        let retryCount = 0;
        let maxRetries = 3;
        let lastFetchedData = {}; // Store latest API response

        return function (page, params) {

            // Listen for visibility changes
            document.addEventListener("visibilitychange", () => {
                if (document.visibilityState === "hidden") {
                    stopMonitoring(); // Stop CPU monitoring when navigating away
                } else if (document.visibilityState === "visible") {
                    console.log("App regained focus, refreshing UI...");
                    startMonitoring(); // Ensure data fetching restarts
                    updateUI(lastFetchedData); // `lastFetchedData` contains real data
                }
            });

            // Start monitoring initially
            startMonitoring();
            window.addEventListener("beforeunload", function (event) {
                console.log("User is leaving the page.");
                // Optional: Show a confirmation dialog
                clearInterval(cpuMonitor);
                //event.preventDefault();
                //event.returnValue = ""; // Some browsers require this for the dialog to appear
            });

            // Stop monitoring when leaving the Emby page
            page.addEventListener('viewhide', () => {
                console.log("User navigated away from the CPU Monitor page.");
                stopMonitoring(); // Stop polling when leaving this Emby section
            });

            // Start monitoring when returning to the page
            page.addEventListener('viewshow', () => {
                console.log("User returned to the CPU Monitor page.");
                startMonitoring();
                Donate(); // Call the Donate function when the page is shown
            });

           

            function fetchCpuUsage() {
                ApiClient.getJSON(ApiClient.getUrl('/CPUMonitorByMick/GetCPUState'))
                    .then(response => {
                        console.log("Emby Response:", response);
                        let parsedResponse = response; // No need to parse again
                        if (!parsedResponse || !parsedResponse.cores) {
                            lastFetchedData = response; // Store response for visibility
                            console.error("Unexpected response structure:", response);
                            if (++retryCount >= maxRetries) {
                                clearInterval(cpuMonitor);
                            }
                            return;
                        }

                        updateUI(response);
                        retryCount = 0; // Reset retry count on success
                    })
                    .catch(e => {
                        console.error("Error during API call:", e);
                        if (++retryCount >= maxRetries) {
                            clearInterval(cpuMonitor);
                        }
                    });
            }
            function updateUI(data) {
                let cpuContainer = document.getElementById("cpuContainer");
                if (!cpuContainer) {
                    console.error("Error: cpuContainer not found.");
                    return;
                }

                console.log("API Response Structure:", data); // Debugging

                if (!data.cores || !Array.isArray(data.cores)) {
                    console.error("Error: Unexpected API structure:", data);
                    cpuContainer.innerHTML = "Error: Invalid API data.";
                    return;
                }

                cpuContainer.innerHTML = ""; // Clear UI

                data.cores.forEach(cpu => {
                    const div = document.createElement("div");
                    div.classList.add("core");

                    let usageValue = cpu.Usage !== undefined ? cpu.Usage.toFixed(1) : "N/A";
                    let usageWidth = cpu.Usage !== undefined ? `${cpu.Usage}%` : "0%";
                    let usageColor = cpu.Usage !== undefined
                        ? `rgb(${cpu.Usage * 2.55}, ${255 - cpu.Usage * 2.55}, 0)`
                        : "gray";

                    div.innerHTML = `
                                    <span class="label">Core ${cpu.Core}:</span>
                                    <div class="progress">
                                        <div class="fill" id="core${cpu.Core}" style="width: ${usageWidth}; background-color: ${usageColor}; transition: width 0.5s ease-in-out;"></div>
                                    </div>
                                    <span id="label${cpu.Core}"> ${usageValue}%</span>
                                `;

                    cpuContainer.appendChild(div);
                });
            }
            function startMonitoring() {
                if (!cpuMonitor) {
                    cpuMonitor = setInterval(fetchCpuUsage, 500);
                    console.log("CPU monitoring started.");
                }
            }

            function stopMonitoring() {
                if (cpuMonitor) {
                    clearInterval(cpuMonitor);
                    cpuMonitor = null;
                    console.log("CPU monitoring stopped.");
                }
            }

            function Donate() {
                ApiClient.getJSON(ApiClient.getUrl('/MechBox/Donate.html/LoadHTMLfromWeb')).then(response => {
                    const donateDiv = page.querySelector("#Donate");
                    if (donateDiv) {
                        donateDiv.innerHTML = response.Text;
                        console.log("Loading Donate HTML file from Web: " + response.Text);

                        // Force a reflow/repaint with a small delay
                        setTimeout(() => {
                            donateDiv.style.display = 'none';
                            donateDiv.offsetHeight; // Trigger reflow
                            donateDiv.style.display = '';
                        }, 10);
                    }
                }).catch(e => {
                    console.log(e);
                });
            }

        }
    }
);

