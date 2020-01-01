﻿namespace Onbox.Mvc.V1
{
    public interface IMessageService
    {
        void Error(string message);
        bool Question(string message);
        void SetTitle(string newTitle);
        void Show(string message);
        void Warning(string message);
    }
}