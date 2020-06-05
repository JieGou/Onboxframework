﻿using Onbox.Mvc.V7;
using Onbox.Revit.V7;

namespace Onbox.Mvc.Revit.V7
{
    /// <summary>
    /// Provides specific Revit functionaliy to <see cref="ViewMvcBase"/> like set Revit as parent window and Title Bar visibility
    /// </summary>
    public abstract class RevitViewMvcBase : ViewMvcBase, IRevitViewMvcBase
    {
        private TitleVisibility titleVisibility = TitleVisibility.HideMinimizeAndMaximize;

        /// <summary>
        /// Provides specific Revit functionaliy to <see cref="ViewMvcBase"/> like set Revit as parent window and Title Bar visibility
        /// </summary>
        /// <param name="revitUIApp">Some information of the current instance of Revit UI App</param>
        public RevitViewMvcBase(IRevitUIApp revitUIApp)
        {
            var attacher = new RevitViewAttacher(this, revitUIApp.GetRevitWindowHandle(), this.titleVisibility);
            attacher.Attach();
        }

        public void SetTitleVisibility(TitleVisibility titleVisibility)
        {
            this.titleVisibility = titleVisibility;
        }
    }
}
