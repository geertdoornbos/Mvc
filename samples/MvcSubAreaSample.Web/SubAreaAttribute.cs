﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Infrastructure;

namespace MvcSubAreaSample.Web
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class SubAreaAttribute : RouteConstraintAttribute
    {
        public SubAreaAttribute(string name)
            : base("subarea", name, blockNonAttributedActions: true)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Area name must not be empty", nameof(name));
            }
        }
    }
}