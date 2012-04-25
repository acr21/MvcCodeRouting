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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Web.Hosting;

namespace MvcCodeRouting {

   sealed class EmbeddedViewsVirtualPathProvider : VirtualPathProvider {

      static readonly StringComparison VirtualPathComparison = StringComparison.OrdinalIgnoreCase;
      static readonly StringComparer VirtualPathComparer = StringComparer.OrdinalIgnoreCase;

      static readonly List<AssemblyResourceData> AssemblyDataTable = new List<AssemblyResourceData>();
      readonly ConcurrentDictionary<string, AssemblyResourceDataCollection> virtualPathCache = new ConcurrentDictionary<string, AssemblyResourceDataCollection>(VirtualPathComparer);
      static bool embeddedViewsEnabled, registered;

      public static void RegisterAssembly(RegisterInfo registerInfo) {

         string basePath = String.Join("/", new[] { "Views", registerInfo.ViewsLocation }.Where(s => !String.IsNullOrEmpty(s)));
         var assemblyData = new AssemblyResourceData(registerInfo, basePath);

         if (assemblyData.HasResources) 
            AssemblyDataTable.Add(assemblyData);

         if (embeddedViewsEnabled && !registered)
            RegisterIfNecessary();
      }

      public static void RegisterIfNecessary() {

         embeddedViewsEnabled = true;

         if (AssemblyDataTable.Count > 0 && !registered) {
            HostingEnvironment.RegisterVirtualPathProvider(new EmbeddedViewsVirtualPathProvider());
            registered = true;
         }
      }

      public override bool DirectoryExists(string virtualDir) {

         bool prevExists = base.DirectoryExists(virtualDir);

         if (prevExists)
            return prevExists;

         return GetAssemblyData(virtualDir).ResourceExists(virtualDir, isFile: false);
      }

      public override bool FileExists(string virtualPath) {
         
         bool prevExists = base.FileExists(virtualPath);

         if (prevExists)
            return prevExists;

         return GetAssemblyData(virtualPath).ResourceExists(virtualPath, isFile: true);
      }

      public override VirtualDirectory GetDirectory(string virtualDir) {

         VirtualDirectory prev = base.GetDirectory(virtualDir);

         string resourceName;
         AssemblyResourceData assemblyData;

         if (GetAssemblyData(virtualDir).ResourceExists(virtualDir, false, out resourceName, out assemblyData))
            return assemblyData.CreateVirtualDirectory(virtualDir, prev);

         return prev;
      }

      public override VirtualFile GetFile(string virtualPath) {

         bool prevExists = base.FileExists(virtualPath);

         if (prevExists)
            return base.GetFile(virtualPath);

         string resourceName;
         AssemblyResourceData assemblyData;

         if (GetAssemblyData(virtualPath).ResourceExists(virtualPath, true, out resourceName, out assemblyData))
            return assemblyData.CreateVirtualFile(virtualPath, resourceName);

         return null;
      }

      public override CacheDependency GetCacheDependency(string virtualPath, IEnumerable virtualPathDependencies, DateTime utcStart) {
         
         bool prevExists = base.FileExists(virtualPath);

         if (prevExists)
            return base.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);

         return null;
      }

      AssemblyResourceDataCollection GetAssemblyData(string virtualPath) {

         string appRelativePath = VirtualPathUtility.ToAppRelative(virtualPath);
         
         if (virtualPath.Length > 0 && virtualPath[0] == '~')
            virtualPath = VirtualPathUtility.ToAbsolute(appRelativePath);

         List<string> parts = appRelativePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).ToList();

         if (parts.Count < 1 || !parts[0].Equals("Views", VirtualPathComparison))
            return AssemblyResourceDataCollection.Null;

         return this.virtualPathCache.GetOrAdd(virtualPath, (s) => {

            bool isFile = parts.Count > 1
               && virtualPath[virtualPath.Length - 1] != '/'
               && parts[parts.Count - 1].IndexOf('.') != -1;

            if (isFile) 
               parts.RemoveAt(parts.Count - 1);

            var result = new AssemblyResourceDataCollection();

            for (int i = 0; i < parts.Count; i++) {
               string basePath = String.Join("/", parts.Take(parts.Count - i));

               result.AddRange(AssemblyDataTable.Where(d => d.BasePath.Equals(basePath, VirtualPathComparison)));

               if (result.Count > 0)
                  break;
            }

            return result;
         });
      }

      class AssemblyResourceData {

         public readonly string BasePath;
         readonly string[] basePathParts;
         readonly string baseResourceName;

         readonly Assembly assembly;
         readonly string assemblyName;
         readonly int assemblyNamePartsCount;
         readonly string[] resourceNames;

         public bool HasResources { get { return resourceNames.Length > 0; } }

         public AssemblyResourceData(RegisterInfo registerInfo, string basePath) {

            this.assembly = registerInfo.Assembly;
            this.assemblyName = this.assembly.GetName().Name;
            this.assemblyNamePartsCount = this.assemblyName.Split('.').Count();
            this.BasePath = basePath;
            this.basePathParts = basePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            this.baseResourceName = RelativeVirtualPathToResourceName(basePath);
            this.resourceNames =
               (from n in assembly.GetManifestResourceNames()
                where n.StartsWith(this.baseResourceName + ".", StringComparison.Ordinal)
                   && n.Split('.').Length >= 4
                select n).ToArray();
         }

         public bool ResourceExists(string virtualPath, bool isFile, out string resourceName) {

            if (this.HasResources) {

               string relativeVirtualPath = VirtualPathUtility.ToAppRelative(virtualPath).Remove(0, 2);
               string[] relativeVirtualPathParts = relativeVirtualPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

               for (int i = 0; i < resourceNames.Length; i++) {

                  resourceName = resourceNames[i];
                  string resourcePath = ResourceNameToRelativeVirtualPath(resourceName);
                  string[] resourcePathParts = resourcePath.Split('/');

                  bool resourcePathLengthOK = (isFile) ?
                     resourcePathParts.Length == relativeVirtualPathParts.Length
                     : resourcePathParts.Length > relativeVirtualPathParts.Length;

                  if (resourcePathLengthOK) {

                     string resourcePath2 = (isFile) ?
                        resourcePath
                        : String.Join("/", resourcePathParts.Take(resourcePathParts.Length - 1));

                     if (String.Equals(String.Join("/", relativeVirtualPathParts), resourcePath2, VirtualPathComparison))
                        return true;
                  }
               }
            }

            resourceName = null;
            return false;
         }

         public string ResourceNameToRelativeVirtualPath(string resourceName) {

            int dotFirstIndex = resourceName.IndexOf('.');
            int dotLastIndex = resourceName.LastIndexOf('.');

            string virtualPath = resourceName;

            if (dotFirstIndex > 0
               && dotFirstIndex != dotLastIndex) {

               StringBuilder sb = new StringBuilder(resourceName);
               sb.Replace('.', '/', 0, dotLastIndex);

               virtualPath = sb.ToString();
            }

            List<string> parts = virtualPath.Split('/').ToList();
            parts.RemoveRange(0, this.assemblyNamePartsCount);
            parts.InsertRange(1, this.basePathParts.Skip(1));

            return String.Join("/", parts);
         }

         public string RelativeVirtualPathToResourceName(string relativeVirtualPath) {

            List<string> parts = relativeVirtualPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            if (parts.Count > 1)
               parts.RemoveRange(1, basePathParts.Length - 1);

            parts.Insert(0, this.assemblyName);

            return String.Join(".", parts);
         }

         public string[] GetFileResources() {

            string[] baseNameParts = this.baseResourceName.Split('.');

            return
               (from resourceName in resourceNames
                let resourceParts = resourceName.Split('.')
                where resourceParts.Length == baseNameParts.Length + 2
                   && String.Equals(String.Join(".", resourceParts.Take(baseNameParts.Length)), this.baseResourceName, StringComparison.Ordinal)
                select resourceName).ToArray();
         }

         public VirtualFile CreateVirtualFile(string virtualPath, string resourceName) {
            return new AssemblyResourceVirtualFile(virtualPath, resourceName, this.assembly);
         }

         public VirtualDirectory CreateVirtualDirectory(string virtualPath, VirtualDirectory prev) {
            return new AssemblyResourceVirtualDirectory(virtualPath, prev, this);
         }
      }

      class AssemblyResourceDataCollection : Collection<AssemblyResourceData> {

         public static readonly AssemblyResourceDataCollection Null = new AssemblyResourceDataCollection();

         public void AddRange(IEnumerable<AssemblyResourceData> items) {

            foreach (var item in items) 
               this.Add(item);
         }

         public bool ResourceExists(string virtualPath, bool isFile) {

            if (this.Count == 0)
               return false;

            string resourceName;
            AssemblyResourceData data;

            return ResourceExists(virtualPath, isFile, out resourceName, out data);
         }

         public bool ResourceExists(string virtualPath, bool isFile, out string resourceName, out AssemblyResourceData data) {

            for (int i = 0; i < this.Count; i++) {

               data = this[i];

               if (data.ResourceExists(virtualPath, isFile, out resourceName))
                  return true;
            }

            resourceName = null;
            data = null;

            return false;
         }
      }
      
      class AssemblyResourceVirtualDirectory : VirtualDirectory {

         readonly VirtualDirectory prevDirectory;
         readonly AssemblyResourceData assemblyData;

         List<VirtualFile> _Files;
         List<VirtualDirectory> _Directories;

         public override IEnumerable Children {
            get {
               return Directories.Cast<object>().Concat(Files.Cast<object>());
            }
         }

         public override IEnumerable Directories {
            get {
               if (_Directories == null) {
                  List<VirtualDirectory> prevDirs = this.prevDirectory.Directories.Cast<VirtualDirectory>().ToList();

                  _Directories = prevDirs;
               }
               return _Directories;
            }
         }

         public override IEnumerable Files {
            get {
               if (_Files == null) {
                  List<VirtualFile> prevFiles = this.prevDirectory.Files.Cast<VirtualFile>().ToList();

                  string[] fileResources = this.assemblyData.GetFileResources();

                  for (int i = 0; i < fileResources.Length; i++) {

                     string resourceName = fileResources[i];
                     string virtualPath = VirtualPathUtility.ToAbsolute("~/" + this.assemblyData.ResourceNameToRelativeVirtualPath(resourceName));

                     if (prevFiles.Exists(v => v.VirtualPath == virtualPath))
                        continue;

                     prevFiles.Add(this.assemblyData.CreateVirtualFile(virtualPath, resourceName));
                  }

                  _Files = prevFiles;
               }
               return _Files;
            }
         }

         public AssemblyResourceVirtualDirectory(string virtualPath, VirtualDirectory prevDirectory, AssemblyResourceData assemblyData)
            : base(virtualPath) {

            this.prevDirectory = prevDirectory;
            this.assemblyData = assemblyData;
         }
      }

      class AssemblyResourceVirtualFile : VirtualFile {

         readonly string resourceName;
         readonly Assembly assembly;

         public AssemblyResourceVirtualFile(string virtualPath, string resourceName, Assembly assembly)
            : base(virtualPath) {

            this.resourceName = resourceName;
            this.assembly = assembly;
         }

         public override Stream Open() {
            return this.assembly.GetManifestResourceStream(resourceName);
         }
      }
   }
}