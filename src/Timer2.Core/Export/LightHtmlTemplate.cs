using System;

namespace Timer2.Core.Export
{
    public class LightHtmlTemplate : IHtmlTemplate
    {
        public string ThemeName => "Light";

        public string GetCss()
        {
            return @"
:root {
    --bg-main: #f8fafc;
    --bg-card: #ffffff;
    --bg-card-hover: #f1f5f9;
    --border-color: #cbd5e1;
    --text-main: #0f172a;
    --text-muted: #64748b;
    --accent: #0284c7;
    --accent-glow: rgba(2, 132, 199, 0.2);
    --badge-bg: #e0f2fe;
    --badge-text: #0369a1;
    --sticky-bg: rgba(255, 255, 255, 0.95);
    --shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
}
body {
    background-color: var(--bg-main);
    color: var(--text-main);
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
    margin: 0;
    padding: 20px;
}
.container {
    max-width: 1200px;
    margin: 0 auto;
}
.header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-bottom: 2px solid var(--border-color);
    padding-bottom: 16px;
    margin-bottom: 24px;
}
.header h1 {
    margin: 0;
    font-size: 1.8rem;
    color: var(--accent);
    display: flex;
    align-items: center;
    gap: 10px;
}
.control-panel {
    background: var(--bg-card);
    border: 1px solid var(--border-color);
    border-radius: 8px;
    padding: 20px;
    margin-bottom: 24px;
    box-shadow: var(--shadow);
}
.filter-row {
    display: flex;
    flex-wrap: wrap;
    gap: 16px;
    align-items: flex-end;
    margin-bottom: 12px;
}
.filter-group {
    display: flex;
    flex-direction: column;
    gap: 6px;
}
.filter-group label {
    font-size: 0.85rem;
    color: var(--text-muted);
    font-weight: 600;
    text-transform: uppercase;
}
input, select, button {
    background: #ffffff;
    color: var(--text-main);
    border: 1px solid var(--border-color);
    padding: 8px 12px;
    border-radius: 6px;
    font-size: 0.95rem;
    outline: none;
    transition: all 0.2s;
}
input:focus, select:focus {
    border-color: var(--accent);
    box-shadow: 0 0 0 2px var(--accent-glow);
}
button {
    background: var(--accent);
    color: #ffffff;
    font-weight: 600;
    cursor: pointer;
    border: none;
}
button:hover {
    filter: brightness(1.1);
}
.sticky-header {
    position: sticky;
    top: 0;
    z-index: 100;
    background: var(--sticky-bg);
    backdrop-filter: blur(8px);
    border: 2px solid var(--accent);
    border-radius: 8px;
    padding: 14px 20px;
    margin-bottom: 16px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    box-shadow: var(--shadow);
}
.range-description {
    font-size: 1.05rem;
    color: var(--text-main);
    font-weight: 600;
}
.totals-display {
    text-align: right;
}
.total-primary {
    font-size: 1.35rem;
    font-weight: 700;
    color: var(--accent);
}
.total-secondary {
    font-size: 0.9rem;
    color: var(--text-muted);
}
table {
    width: 100%;
    border-collapse: collapse;
    background: var(--bg-card);
    border-radius: 8px;
    overflow: hidden;
    box-shadow: var(--shadow);
}
th, td {
    padding: 12px 16px;
    text-align: left;
    border-bottom: 1px solid var(--border-color);
}
th {
    background: #f1f5f9;
    color: #475569;
    font-size: 0.85rem;
    text-transform: uppercase;
    font-weight: 600;
}
tr:hover {
    background: var(--bg-card-hover);
}
.badge {
    display: inline-block;
    padding: 3px 8px;
    border-radius: 9999px;
    font-size: 0.75rem;
    font-weight: 600;
    background: var(--badge-bg);
    color: var(--badge-text);
}
.badge-mod {
    background: #fef3c7;
    color: #92400e;
}
";
        }
    }
}
