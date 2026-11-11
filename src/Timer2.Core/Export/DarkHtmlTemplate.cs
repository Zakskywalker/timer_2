using System;

namespace Timer2.Core.Export
{
    public class DarkHtmlTemplate : IHtmlTemplate
    {
        public string ThemeName => "Dark";

        public string GetCss()
        {
            return @"
:root {
    --bg-main: #0b0f19;
    --bg-card: #111827;
    --bg-card-hover: #1f2937;
    --border-color: #374151;
    --text-main: #f9fafb;
    --text-muted: #9ca3af;
    --accent: #10b981;
    --accent-glow: rgba(16, 185, 129, 0.2);
    --badge-bg: #064e3b;
    --badge-text: #6ee7b7;
    --sticky-bg: rgba(17, 24, 39, 0.95);
    --shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.5), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
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
    background: #1f2937;
    color: #f3f4f6;
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
    color: #0b0f19;
    font-weight: 600;
    cursor: pointer;
    border: none;
}
button:hover {
    filter: brightness(1.15);
}
.sticky-header {
    position: sticky;
    top: 0;
    z-index: 100;
    background: var(--sticky-bg);
    backdrop-filter: blur(8px);
    border: 1px solid var(--accent);
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
    color: #e5e7eb;
    font-weight: 500;
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
    background: #1f2937;
    color: #9ca3af;
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
    background: #78350f;
    color: #fde68a;
}
";
        }
    }
}
