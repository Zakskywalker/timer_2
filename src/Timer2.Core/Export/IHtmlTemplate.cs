using System;

namespace Timer2.Core.Export
{
    public interface IHtmlTemplate
    {
        string ThemeName { get; }
        string GetCss();
    }
}
