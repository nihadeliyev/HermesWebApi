﻿using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace HermesWebApi
{
    public class ApiExplorerIgnores : IActionModelConvention
    {
        public void Apply(ActionModel action)
        {
            if (action.Controller.ControllerName.Equals("Pwa"))
                action.ApiExplorer.IsVisible = false;
        }
    }
}
