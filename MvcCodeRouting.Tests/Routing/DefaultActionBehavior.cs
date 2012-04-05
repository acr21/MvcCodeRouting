﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace MvcCodeRouting.Tests.Routing {

   [TestClass]
   public class DefaultActionBehavior {

      RouteCollection routes;
      UrlHelper Url;

      [TestInitialize]
      public void Init() {

         this.routes = new RouteCollection();
         this.Url = TestUtil.CreateUrlHelper(routes);
      }

      [TestMethod]
      public void CanUseEmptyStringInUrlGeneration() {

         // #32
         // Using an empty string as action for URL generation (e.g. Url.Action("")) does not work

         Type controller = typeof(DefaultAction1Controller);

         routes.Clear();
         routes.MapCodeRoutes(controller, new CodeRoutingSettings { RootOnly = true });

         Assert.AreEqual(Url.Action("", controller), "/");
         Assert.AreEqual(routes.At(0).Defaults["action"] as string, "Index");

         controller = typeof(DefaultAction2Controller);

         routes.Clear();
         routes.MapCodeRoutes(controller, new CodeRoutingSettings { RootOnly = true });

         Assert.AreEqual(Url.Action("", controller), "/");
         Assert.AreEqual(routes.At(0).Defaults["action"] as string, "Index");
      }

      [TestMethod]
      public void CanHaveOptionalRouteParameters() {

         // #783
         // Default action with optional route parameters does not work

         Type controller = typeof(DefaultAction3Controller);

         routes.Clear();
         routes.MapCodeRoutes(controller, new CodeRoutingSettings { RootOnly = true });

         Assert.AreEqual(Url.Action("", controller), "/");
         Assert.AreEqual(Url.Action("", controller, new { id = 5 }), "/Index/5");
         Assert.AreEqual(routes.At(0).Defaults["action"] as string, "Index");
      }

      [TestMethod]
      public void CanBeOverloaded() {

         // #535
         // Overloaded default action should not produced a route with hardcoded action

         Type controller = typeof(DefaultAction4Controller);

         routes.Clear();
         routes.MapCodeRoutes(controller, new CodeRoutingSettings { RootOnly = true });

         Assert.AreEqual(Url.Action("", controller), "/");
         Assert.IsTrue(routes.At(0).Url.EndsWith("{action}"));
         Assert.AreEqual(routes.At(0).Defaults["action"] as string, "Index");
      }
   }

   public class DefaultAction1Controller : Controller {
      public void Index() { }
   }

   public class DefaultAction2Controller : Controller {
      public void Index() { }
      public void Foo() { }
   }

   public class DefaultAction3Controller : Controller {
      public void Index([FromRoute]int? id) { }
   }

   public class DefaultAction4Controller : Controller {
      
      public void Index() { }
      
      [HttpPost]
      public void Index(string foo) { }
   }
}