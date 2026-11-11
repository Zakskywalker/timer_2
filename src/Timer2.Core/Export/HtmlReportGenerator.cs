using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Timer2.Core.Calculations;
using Timer2.Core.Models;

namespace Timer2.Core.Export
{
    public class HtmlReportGenerator
    {
        private readonly Dictionary<string, IHtmlTemplate> _templates = new Dictionary<string, IHtmlTemplate>(StringComparer.OrdinalIgnoreCase);

        public HtmlReportGenerator()
        {
            RegisterTemplate(new DarkHtmlTemplate());
            RegisterTemplate(new LightHtmlTemplate());
        }

        public void RegisterTemplate(IHtmlTemplate template)
        {
            _templates[template.ThemeName] = template;
        }

        public string GenerateReport(List<DaySummary> days, string themeName = "Dark")
        {
            if (!_templates.TryGetValue(themeName, out var template))
            {
                template = _templates["Dark"];
            }

            // Flatten all entries for client-side tabular presentation
            var flatEntries = new List<object>();
            foreach (var day in days.OrderBy(d => d.Date))
            {
                foreach (var entry in day.Entries)
                {
                    flatEntries.Add(new
                    {
                        id = entry.Id,
                        date = day.Date.ToString("yyyy-MM-dd"),
                        displayDate = day.Date.ToString("MM/dd/yyyy"),
                        runNumber = entry.RunNumber,
                        startIso = entry.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                        displayStart = entry.StartTime.ToString("MM/dd/yyyy hh:mm:ss tt"),
                        stopIso = entry.StopTime.HasValue ? entry.StopTime.Value.ToString("yyyy-MM-ddTHH:mm:ss") : null,
                        displayStop = entry.StopTime.HasValue ? entry.StopTime.Value.ToString("MM/dd/yyyy hh:mm:ss tt") : "Missing / Running",
                        durationSeconds = (int)entry.Duration.TotalSeconds,
                        displayDuration = entry.StopTime.HasValue ? TimeCalculator.FormatHM(entry.Duration, true) : "0 Hrs. 0 Min. (Missing)",
                        decimalHours = entry.DecimalHours,
                        isModified = entry.IsModified,
                        hasWarning = entry.HasWarning,
                        warningType = entry.WarningType,
                        warningDetails = entry.WarningDetails,
                        notes = entry.Notes ?? ""
                    });
                }
            }

            string jsonData = JsonSerializer.Serialize(flatEntries);
            string css = template.GetCss();

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"UTF-8\">");
            sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine($"    <title>Time Recorder - Log Report ({template.ThemeName})</title>");
            sb.AppendLine("    <style>");
            sb.AppendLine(css);
            sb.AppendLine(@"
.warning-banner {
    background: #451a03;
    border: 1px solid #d97706;
    color: #fef3c7;
    border-radius: 8px;
    padding: 12px 18px;
    margin-bottom: 16px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    box-shadow: var(--shadow);
    animation: fadeIn 0.3s ease;
}
.warning-actions {
    display: flex;
    gap: 8px;
}
.btn-warning-action {
    background: #d97706;
    color: #1c1917;
    font-size: 0.85rem;
    padding: 6px 12px;
    border-radius: 4px;
    font-weight: 700;
}
.btn-warning-action:hover {
    background: #f59e0b;
}
.btn-omit-active {
    background: #ef4444 !important;
    color: white !important;
}
.badge-warning {
    background: #b45309;
    color: #fef3c7;
    cursor: help;
}
tr.has-warning {
    background: rgba(217, 119, 6, 0.08);
}
@keyframes fadeIn {
    from { opacity: 0; transform: translateY(-4px); }
    to { opacity: 1; transform: translateY(0); }
}
");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <div class=\"container\">");
            sb.AppendLine("        <div class=\"header\">");
            sb.AppendLine("            <h1>⏱️ Time Recorder — Master Log Report</h1>");
            sb.AppendLine($"            <div style=\"color: var(--text-muted); font-size: 0.9rem;\">Generated on: {DateTime.Now:MM/dd/yyyy hh:mm tt}</div>");
            sb.AppendLine("        </div>");
            sb.AppendLine("");
            sb.AppendLine("        <div class=\"control-panel\">");
            sb.AppendLine("            <div class=\"filter-row\">");
            sb.AppendLine("                <div class=\"filter-group\" style=\"flex: 2; min-width: 260px;\">");
            sb.AppendLine("                    <label for=\"searchInput\">Live Search (Date, Time, Notes)</label>");
            sb.AppendLine("                    <input type=\"text\" id=\"searchInput\" placeholder=\"Type to search...\">");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <div class=\"filter-group\">");
            sb.AppendLine("                    <label for=\"startDate\">Start Date</label>");
            sb.AppendLine("                    <input type=\"date\" id=\"startDate\">");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <div class=\"filter-group\">");
            sb.AppendLine("                    <label for=\"startTime\">Start Time</label>");
            sb.AppendLine("                    <input type=\"time\" id=\"startTime\" value=\"00:00\">");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <div class=\"filter-group\">");
            sb.AppendLine("                    <label for=\"endDate\">End Date</label>");
            sb.AppendLine("                    <input type=\"date\" id=\"endDate\">");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <div class=\"filter-group\">");
            sb.AppendLine("                    <label for=\"endTime\">End Time</label>");
            sb.AppendLine("                    <input type=\"time\" id=\"endTime\" value=\"23:59\">");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <div class=\"filter-group\">");
            sb.AppendLine("                    <button id=\"resetBtn\" type=\"button\" style=\"height: 38px;\">Reset Filters</button>");
            sb.AppendLine("                </div>");
            sb.AppendLine("            </div>");
            sb.AppendLine("        </div>");
            sb.AppendLine("");
            sb.AppendLine("        <!-- Dynamic Warning Notification Banner -->");
            sb.AppendLine("        <div id=\"warningBanner\" class=\"warning-banner\" style=\"display: none;\">");
            sb.AppendLine("            <div id=\"warningText\">⚠️ <strong>Warning:</strong> Incomplete times detected in this range.</div>");
            sb.AppendLine("            <div class=\"warning-actions\">");
            sb.AppendLine("                <button type=\"button\" id=\"btnOmitWarnings\" class=\"btn-warning-action\">Omit Incomplete Sessions</button>");
            sb.AppendLine("                <button type=\"button\" id=\"btnFilterOnlyWarnings\" class=\"btn-warning-action\">Filter Warnings Only</button>");
            sb.AppendLine("            </div>");
            sb.AppendLine("        </div>");
            sb.AppendLine("");
            sb.AppendLine("        <div class=\"sticky-header\">");
            sb.AppendLine("            <div class=\"range-description\" id=\"rangeDesc\">Loading log sessions...</div>");
            sb.AppendLine("            <div class=\"totals-display\">");
            sb.AppendLine("                <div class=\"total-primary\" id=\"totalPrimary\">0 Hrs. 0 Min. 0 Sec.</div>");
            sb.AppendLine("                <div class=\"total-secondary\" id=\"totalDecimal\">Decimal: 0.00 hrs</div>");
            sb.AppendLine("            </div>");
            sb.AppendLine("        </div>");
            sb.AppendLine("");
            sb.AppendLine("        <table>");
            sb.AppendLine("            <thead>");
            sb.AppendLine("                <tr>");
            sb.AppendLine("                    <th>Date</th>");
            sb.AppendLine("                    <th>Run</th>");
            sb.AppendLine("                    <th>Start Time</th>");
            sb.AppendLine("                    <th>Stop Time</th>");
            sb.AppendLine("                    <th>Duration (H:M:S)</th>");
            sb.AppendLine("                    <th>Decimal Hours</th>");
            sb.AppendLine("                    <th>Status</th>");
            sb.AppendLine("                    <th>Notes</th>");
            sb.AppendLine("                </tr>");
            sb.AppendLine("            </thead>");
            sb.AppendLine("            <tbody id=\"logTableBody\">");
            sb.AppendLine("            </tbody>");
            sb.AppendLine("        </table>");
            sb.AppendLine("    </div>");
            sb.AppendLine("");
            sb.AppendLine("    <script>");
            sb.AppendLine($"        const rawLogs = {jsonData};");
            sb.AppendLine(@"
        let omitWarningsActive = false;
        let onlyWarningsActive = false;

        function init() {
            if (rawLogs.length > 0) {
                const dates = rawLogs.map(r => r.date).sort();
                document.getElementById('startDate').value = dates[0];
                document.getElementById('endDate').value = dates[dates.length - 1];
            }
            applyFilters();

            document.getElementById('searchInput').addEventListener('input', applyFilters);
            document.getElementById('startDate').addEventListener('change', applyFilters);
            document.getElementById('startTime').addEventListener('change', applyFilters);
            document.getElementById('endDate').addEventListener('change', applyFilters);
            document.getElementById('endTime').addEventListener('change', applyFilters);
            document.getElementById('resetBtn').addEventListener('click', resetFilters);

            document.getElementById('btnOmitWarnings').addEventListener('click', () => {
                omitWarningsActive = !omitWarningsActive;
                const btn = document.getElementById('btnOmitWarnings');
                if (omitWarningsActive) {
                    btn.classList.add('btn-omit-active');
                    btn.textContent = 'Including All (Click to Omit)';
                } else {
                    btn.classList.remove('btn-omit-active');
                    btn.textContent = 'Omit Incomplete Sessions';
                }
                applyFilters();
            });

            document.getElementById('btnFilterOnlyWarnings').addEventListener('click', () => {
                onlyWarningsActive = !onlyWarningsActive;
                const btn = document.getElementById('btnFilterOnlyWarnings');
                btn.textContent = onlyWarningsActive ? 'Show All in Range' : 'Filter Warnings Only';
                applyFilters();
            });
        }

        function resetFilters() {
            document.getElementById('searchInput').value = '';
            omitWarningsActive = false;
            onlyWarningsActive = false;
            document.getElementById('btnOmitWarnings').classList.remove('btn-omit-active');
            document.getElementById('btnOmitWarnings').textContent = 'Omit Incomplete Sessions';
            document.getElementById('btnFilterOnlyWarnings').textContent = 'Filter Warnings Only';

            if (rawLogs.length > 0) {
                const dates = rawLogs.map(r => r.date).sort();
                document.getElementById('startDate').value = dates[0];
                document.getElementById('endDate').value = dates[dates.length - 1];
            }
            document.getElementById('startTime').value = '00:00';
            document.getElementById('endTime').value = '23:59';
            applyFilters();
        }

        function applyFilters() {
            const query = (document.getElementById('searchInput').value || '').toLowerCase();
            const sDate = document.getElementById('startDate').value;
            const sTime = document.getElementById('startTime').value || '00:00';
            const eDate = document.getElementById('endDate').value;
            const eTime = document.getElementById('endTime').value || '23:59';

            const startThreshold = sDate ? new Date(`${sDate}T${sTime}:00`) : null;
            const endThreshold = eDate ? new Date(`${eDate}T${eTime}:59`) : null;

            let totalSeconds = 0;
            let warningsInRange = 0;
            const tbody = document.getElementById('logTableBody');
            tbody.innerHTML = '';

            let count = 0;
            let minVisibleDate = null;
            let maxVisibleDate = null;

            rawLogs.forEach(entry => {
                const entryStart = new Date(entry.startIso);
                if (startThreshold && entryStart < startThreshold) return;
                if (endThreshold && entryStart > endThreshold) return;

                if (query) {
                    const textContent = `${entry.displayDate} ${entry.displayStart} ${entry.displayStop} ${entry.notes} ${entry.warningDetails}`.toLowerCase();
                    if (!textContent.includes(query)) return;
                }

                if (entry.hasWarning) {
                    warningsInRange++;
                }

                if (onlyWarningsActive && !entry.hasWarning) {
                    return;
                }

                if (omitWarningsActive && entry.hasWarning) {
                    return; // Skip from table and calculation
                }

                count++;
                totalSeconds += entry.durationSeconds;

                if (!minVisibleDate || entryStart < minVisibleDate) minVisibleDate = entryStart;
                if (!maxVisibleDate || entryStart > maxVisibleDate) maxVisibleDate = entryStart;

                const tr = document.createElement('tr');
                if (entry.hasWarning) {
                    tr.classList.add('has-warning');
                }

                let badgeHtml = '';
                if (entry.hasWarning) {
                    badgeHtml = `<span class=""badge badge-warning"" title=""${entry.warningDetails}"">⚠️ Incomplete / Fix</span> `;
                }
                if (entry.isModified) {
                    badgeHtml += '<span class=""badge badge-mod"">Modified</span>';
                }
                if (!badgeHtml) {
                    badgeHtml = '<span class=""badge"">Recorded</span>';
                }

                tr.innerHTML = `
                    <td><strong>${entry.displayDate}</strong></td>
                    <td>Run ${entry.runNumber}</td>
                    <td>${entry.displayStart}</td>
                    <td>${entry.displayStop}</td>
                    <td>${entry.displayDuration}</td>
                    <td><strong>${entry.decimalHours.toFixed(2)} hrs</strong></td>
                    <td>${badgeHtml}</td>
                    <td>${entry.warningDetails || entry.notes || '-'}</td>
                `;
                tbody.appendChild(tr);
            });

            // Update warning notification banner
            const banner = document.getElementById('warningBanner');
            if (warningsInRange > 0) {
                banner.style.display = 'flex';
                document.getElementById('warningText').innerHTML = `⚠️ <strong>Warning:</strong> ${warningsInRange} session(s) in this selected range have missing or incomplete times.`;
            } else {
                banner.style.display = 'none';
            }

            // Update sticky summary
            const hrs = Math.floor(totalSeconds / 3600);
            const mins = Math.floor((totalSeconds % 3600) / 60);
            const secs = totalSeconds % 60;
            const decHours = (totalSeconds / 3600).toFixed(2);

            document.getElementById('totalPrimary').textContent = `${hrs} Hrs. ${mins} Min. ${secs} Sec.`;
            const omitNotice = omitWarningsActive ? ' (Incomplete sessions omitted)' : '';
            document.getElementById('totalDecimal').textContent = `Total: ${decHours} hrs${omitNotice}`;

            const rangeDesc = document.getElementById('rangeDesc');
            if (count === 0) {
                rangeDesc.textContent = 'No matching timer logs found';
            } else {
                const minStr = minVisibleDate.toLocaleDateString();
                const maxStr = maxVisibleDate.toLocaleDateString();
                rangeDesc.textContent = `Showing ${count} session(s) [${minStr} – ${maxStr}]`;
            }
        }

        window.onload = init;
    </script>
</body>
</html>");

            return sb.ToString();
        }

        public void SaveReportToFile(List<DaySummary> days, string filePath, string themeName = "Dark")
        {
            string html = GenerateReport(days, themeName);
            File.WriteAllText(filePath, html, Encoding.UTF8);
        }
    }
}
