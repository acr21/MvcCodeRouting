﻿// Copyright 2011 Max Toro Q.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Globalization;
using MvcCodeRouting.Controllers;
using System.Web.Http;

namespace MvcCodeRouting {
   
   class RegisterSettings {

      readonly Func<Type, bool> isSupportedController;
      string _BaseRoute;
      Assembly _Assembly;
      CodeRoutingSettings _Settings;
      string _RootNamespace;
      string _ViewsLocation;
      object _HttpConfiguration;

      public string BaseRoute {
         get { return _BaseRoute; }
         set {
            if (!String.IsNullOrWhiteSpace(value)) {
               value = value.Trim();

               if (new[] { '~', '/' }.Any(c => value[0] == c)) {
                  throw new ArgumentException(
                     String.Format(CultureInfo.InvariantCulture, "Base route cannot start with '{0}'.", value[0])
                  );
               }

               _BaseRoute = value;
            }
         }
      }

      public Assembly Assembly {
         get {
            if (_Assembly == null) 
               _Assembly = this.RootController.Assembly;
            return _Assembly;
         }
         private set {
            _Assembly = value;
         }
      }

      public Type RootController { get; private set; }
      
      public CodeRoutingSettings Settings {
         get {
            if (_Settings == null) 
               _Settings = new CodeRoutingSettings();
            return _Settings;
         }
         set { _Settings = value; }
      }

      public string RootNamespace {
         get {
            if (_RootNamespace == null) {
               if (this.RootController != null) {
                  _RootNamespace = this.RootController.Namespace;
               } else {
                  throw new InvalidOperationException();
               }
            }
            return _RootNamespace;
         }
      }

      public string ViewsLocation {
         get {
            if (_ViewsLocation == null) {
               _ViewsLocation = (this.BaseRoute != null) ? 
                  String.Join("/", this.BaseRoute.Split('/').Where(s => !s.Contains('{'))) 
                  : "";
            }
            return _ViewsLocation;
         }
      }

      public object HttpConfiguration {
         get {
            if (_HttpConfiguration == null)
               return GlobalConfiguration.Configuration;
            return _HttpConfiguration;
         }
         set { _HttpConfiguration = value; }
      }

      public RegisterSettings(Assembly assembly, Type rootController, Func<Type, bool> isSupportedController) {

         if (isSupportedController == null) throw new ArgumentNullException("isSupportedController");

         this.isSupportedController = isSupportedController;

         if (rootController != null) { 
            
            if (!IsValidControllerType(rootController))
               throw new InvalidOperationException("The specified root controller is not a valid controller type.");

            if (assembly != null && rootController.Assembly != assembly)
               throw new InvalidOperationException("The specified root controller does not belong to the specified assembly.");
         
         } else if (assembly == null) {
            throw new ArgumentException("Either assembly or rootController must be specified.");
         }

         this.Assembly = assembly;
         this.RootController = rootController;
      }

      public IEnumerable<ControllerInfo> GetControllers() {

         return
            from t in GetControllerTypes()
            where !this.Settings.IgnoredControllers.Contains(t)
            let c = CreateControllerInfo(t)
            where c.IsInRootNamespace
            select c;
      }

      IEnumerable<Type> GetControllerTypes() {

         Type[] types = (this.Settings.RootOnly) ?
            new[] { this.RootController }
            : this.Assembly.GetTypes();

         return types.Where(t => IsValidControllerType(t));
      }

      bool IsValidControllerType(Type type) {

         return type.IsPublic
            && !type.IsAbstract
            && type.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase)
            && isSupportedController(type);
      }

      ControllerInfo CreateControllerInfo(Type controllerType) {

         if (Web.Mvc.MvcControllerInfo.IsMvcController(controllerType))
            return Web.Mvc.MvcControllerInfo.Create(controllerType, this);

         return Web.Http.HttpControllerInfo.Create(controllerType, this);
      }
   }
}
