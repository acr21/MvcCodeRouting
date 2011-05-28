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
using System.Web.Mvc;
using System.Web.Routing;

namespace MvcCodeRouting {
   
   public static class RouteDebugExtensions {

      public static string ToCSharpMapRouteCalls(this RouteCollection routes) {

         if (routes == null) throw new ArgumentNullException("routes");

         StringBuilder sb = new StringBuilder();
         sb.Append("<html>"); ;
         AppendHtmlHead(sb);
         sb.Append("<body class='csharp'>");

         foreach (Route item in routes.OfType<Route>()) {

            string mapRoute = item.ToCSharpMapRouteCall();

            if (!String.IsNullOrEmpty(mapRoute)) {
               sb.Append(mapRoute)
                  .AppendLine()
                  .AppendLine();
            }
         }

         sb.Append("</body>")
            .Append("</html>");

         return sb.ToString();
      }

      static string ToCSharpMapRouteCall(this Route route) {

         if (route == null) throw new ArgumentNullException("route");

         StringBuilder sb = new StringBuilder();

         Type handlerType = route.RouteHandler.GetType();

         if (typeof(StopRoutingHandler).IsAssignableFrom(handlerType)) {

            sb.AppendFormat("routes.IgnoreRoute(<span class='string'>\"{0}\"</span>);", route.Url);

         } else if (typeof(MvcRouteHandler).IsAssignableFrom(handlerType)) {

            sb.AppendFormat("routes.MapRoute(<span class='keyword'>null</span>, <span class='string'>\"{0}\"</span>", route.Url);

            int i = 0;

            if (route.Defaults != null && route.Defaults.Count > 0) {

               sb.Append(", ")
                  .AppendLine()
                  .Append("    <span class='keyword'>new</span> { ");

               foreach (var item in route.Defaults) {

                  if (i > 0)
                     sb.Append(", ");

                  sb.AppendFormat("{0} = {1}", item.Key, ValueToCSharpString(item.Value));

                  i++;
               }

               sb.Append(" }");

               if (route.Constraints != null && route.Constraints.Count > 0) {

                  sb.Append(", ")
                        .AppendLine()
                        .Append("    <span class='keyword'>new</span> { ");

                  int j = 0;

                  foreach (var item in route.Constraints) {

                     if (j > 0)
                        sb.Append(", ");

                     sb.AppendFormat("{0} = {1}", item.Key, ValueToCSharpString(item.Value, constraint: true));

                     j++;
                  }

                  sb.Append(" }");
               }
            }

            string[] namespaces;

            if (route.DataTokens != null && (namespaces = route.DataTokens[DataTokenKeys.Namespaces] as string[]) != null) {

               sb.Append(", ")
                  .AppendLine()
                  .Append("    <span class='keyword'>new</span>[] { ");

               for (int j = 0; j < namespaces.Length; j++) {
                  if (j > 0)
                     sb.Append(", ");

                  sb.Append("<span class='string'>\"")
                     .Append(namespaces[j])
                     .Append("\"</span>");
               }

               sb.Append(" }");
            }

            sb.Append(");");

#if DEBUG
            string baseRoute = route.DataTokens[DataTokenKeys.ControllerBaseRoute] as string;

            if (baseRoute != null)
               sb.AppendFormat(" <span class='comment'>// {0}: \"{1}\"</span>", DataTokenKeys.ControllerBaseRoute, baseRoute); 
#endif
         }

         return sb.ToString();
      }

      static string ValueToCSharpString(object val, bool constraint = false) {

         string stringVal;

         if (val == null)
            stringVal = "<span class='keyword'>null</span>";

         else if (val.GetType() == typeof(string))
            stringVal = String.Concat("<span class='string'>@\"", val, "\"</span>");

         else if (val.GetType() == typeof(UrlParameter))
            stringVal = "<span class='type'>UrlParameter</span>.Optional";

         else if (constraint)
            stringVal = String.Concat("<span class='keyword'>new</span> ", val.GetType().FullName, "()");

         else
            stringVal = val.ToString();

         return stringVal;
      }

      public static string ToVBMapRouteCalls(this RouteCollection routes) {

         if (routes == null) throw new ArgumentNullException("routes");

         StringBuilder sb = new StringBuilder();
         sb.Append("<html>"); ;
         AppendHtmlHead(sb);
         sb.Append("<body class='vb'>");

         foreach (Route item in routes.OfType<Route>()) {

            string mapRoute = item.ToVBMapRouteCall();

            if (!String.IsNullOrEmpty(mapRoute)) {
               sb.Append(mapRoute)
                  .AppendLine()
                  .AppendLine();
            }
         }

         sb.Append("</body>")
            .Append("</html>");

         return sb.ToString();
      }

      static string ToVBMapRouteCall(this Route route) {

         if (route == null) throw new ArgumentNullException("route");

         StringBuilder sb = new StringBuilder();

         Type handlerType = route.RouteHandler.GetType();

         if (typeof(StopRoutingHandler).IsAssignableFrom(handlerType)) {

            sb.AppendFormat("routes.IgnoreRoute(<span class='string'>\"{0}\"</span>)", route.Url);

         } else if (typeof(MvcRouteHandler).IsAssignableFrom(handlerType)) {

            sb.AppendFormat("routes.MapRoute(<span class='keyword'>Nothing</span>, <span class='string'>\"{0}\"</span>", route.Url);

            int i = 0;

            if (route.Defaults != null && route.Defaults.Count > 0) {

               sb.Append(", _")
                  .AppendLine()
                  .Append("    <span class='keyword'>New With</span> {");

               foreach (var item in route.Defaults) {

                  if (i > 0)
                     sb.Append(", ");

                  sb.AppendFormat(".{0} = {1}", item.Key, ValueToVBString(item.Value));

                  i++;
               }

               sb.Append("}");

               if (route.Constraints != null && route.Constraints.Count > 0) {

                  sb.Append(", _")
                        .AppendLine()
                        .Append("    <span class='keyword'>New With</span> {");

                  int j = 0;

                  foreach (var item in route.Constraints) {

                     if (j > 0)
                        sb.Append(", ");

                     sb.AppendFormat(".{0} = {1}", item.Key, ValueToVBString(item.Value, constraint: true));

                     j++;
                  }

                  sb.Append("}");
               }
            }

            string[] namespaces;

            if (route.DataTokens != null && (namespaces = route.DataTokens[DataTokenKeys.Namespaces] as string[]) != null) {

               sb.Append(", _")
                  .AppendLine()
                  .Append("    <span class='keyword'>New String</span>() {");

               for (int j = 0; j < namespaces.Length; j++) {
                  if (j > 0)
                     sb.Append(", ");

                  sb.Append("<span class='string'>\"")
                     .Append(namespaces[j])
                     .Append("\"</span>");
               }

               sb.Append("}");
            }

            sb.Append(")");

            string baseRoute = route.DataTokens[DataTokenKeys.ControllerBaseRoute] as string;

#if DEBUG
            if (baseRoute != null)
               sb.AppendFormat(" <span class='comment'>' {0}: \"{1}\"</span>", DataTokenKeys.ControllerBaseRoute, baseRoute);
#endif
         }

         return sb.ToString();
      }

      static string ValueToVBString(object val, bool constraint = false) {

         string stringVal;

         if (val == null)
            stringVal = "<span class='keyword'>Nothing<span>";

         else if (val.GetType() == typeof(string))
            stringVal = String.Concat("<span class='string'>\"", val, "\"</span>");

         else if (val.GetType() == typeof(UrlParameter))
            stringVal = "<span class='type'>UrlParameter</span>.Optional";

         else if (constraint)
            stringVal = String.Concat("<span class='keyword'>New</span> ", val.GetType().FullName, "()");

         else
            stringVal = val.ToString();

         return stringVal;
      }

      static void AppendHtmlHead(StringBuilder sb) {

         sb.Append("<head>")
            .Append("<style type='text/css'>")
            .AppendLine("body { white-space: pre; font-family: Consolas, 'Courier New'; font-size: 80%; }")
            .AppendLine(".comment { color: #008000; }")
            .AppendLine(".string { color: #ac1414; }")
            .AppendLine(".keyword { color: #0026fd; }")
            .AppendLine(".type { color: 2b91af; }")
            .Append("</style>")
            .Append("</head>")
            ;
      }
   }
}