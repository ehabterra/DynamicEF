using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DynamicEF.DAL
{
    public static class ManageAssembly
    {
        const string sourceDir = "DynamicClasses"; // Source code folder within current assembly path
        const string targetDir = "DynamicDlls"; // Generated dlls folder (every file in sourceDir has it's own dll)
        const string codeBasePre = @"file:///"; // for codebase trimming to get current assembly path
        const string compilerType = "CSharp"; // you can change it to vb as you want

        private static Dictionary<string, Type> TypesCache;

        /// <summary>
        /// Build dlls from classes in the source directory
        /// </summary>
        public static void CreateAssemblies()
        {

            try
            {
                string sourceLocation = "";
                string targetLocation = "";
                string location = "";

                GetWorkPaths(out sourceLocation, out targetLocation, out location);

                foreach (var item in Directory.GetFiles(sourceLocation))
                {
                    var fileName = Path.GetFileNameWithoutExtension(item);

                    var targetFile = Path.Combine(targetLocation, fileName + ".dll");

                    CodeDomProvider codeProvider = CodeDomProvider.CreateProvider(compilerType); // I used CSharp engine you can use VB
                    CompilerParameters parameters = new CompilerParameters();
                    parameters.GenerateExecutable = false;
                    parameters.OutputAssembly = targetFile;

                    #region this part for adding references to generate dll

                    // Current assembly if you want to comment no problem
                    parameters.ReferencedAssemblies.Add(location);

                    // To support Linq
                    parameters.ReferencedAssemblies.Add("System.Core.dll");

                    // Data Annotations to support attributes for Entity Framework
                    parameters.ReferencedAssemblies.Add("System.ComponentModel.DataAnnotations.dll");

                    // To support attributes for Services serialization such as "DataContract"
                    parameters.ReferencedAssemblies.Add("System.Runtime.Serialization.dll");

                    // To support relations between class and other already generated assemblies
                    foreach (var reference in Directory.GetFiles(targetLocation))
                    {
                        if (reference != targetFile)
                        {
                            parameters.ReferencedAssemblies.Add(reference);
                        }
                    }
                    #endregion


                    // Compile File
                    CompilerResults results = codeProvider.CompileAssemblyFromSource(parameters, File.ReadAllText(item));

                    // If there are errors display them as exception
                    if (results.Errors.HasErrors)
                    {

                        CompilerError[] errors = new CompilerError[results.Errors.Count];

                        results.Errors.CopyTo(errors, 0);

                        List<CompilerError> errorList = new List<CompilerError>(errors);

                        throw new Exception(string.Format("Compile Errors for: {0}.\nDetails: {1}", item, string.Join("\n", errorList.Select(x => x.ErrorText).ToArray())));
                    }
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// Get Paths for current dll, source directory and target directory
        /// </summary>
        /// <param name="sourceLocation">for source code</param>
        /// <param name="targetLocation">for generated dlls</param>
        /// <param name="location">current assembly location</param>
        public static void GetWorkPaths(out string sourceLocation, out string targetLocation, out string location)
        {
            // Get path for current assembly
            location = Assembly.GetExecutingAssembly().CodeBase.Replace(codeBasePre, "");

            sourceLocation = Path.Combine(Path.GetDirectoryName(location), sourceDir);

            targetLocation = Path.Combine(Path.GetDirectoryName(location), targetDir);

            // Build Directory path (if not exists) to set generated files
            Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(location), targetDir));
        }

        /// <summary>
        /// Get types (classes) from assembly
        /// </summary>
        /// <param name="fileName">filename within Target Directory</param>
        /// <returns>all types in the assembly in list</returns>
        public static IEnumerable<Type> GetDynamicTypes(string fileName)
        {
            IEnumerable<Type> types;
            Assembly asm;

            asm = Assembly.LoadFrom(fileName);
            types = asm.ExportedTypes;
            return types;
        }

        /// <summary>
        /// Get all files generated dynamically
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAllDynamicAssemblyNames()
        {
            string location;
            string targetLocation;

            // Get path of the current assembly to use it to get all generated assemblies
            location = Assembly.GetExecutingAssembly().CodeBase.Replace(codeBasePre, "");
            targetLocation = Path.Combine(Path.GetDirectoryName(location), targetDir);

            // get dll files only
            return Directory.GetFiles(targetLocation, "*.dll").ToList();
        }

        /// <summary>
        /// Get all types in dynamic generated assemblies directory 
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Type> GetAllDynamicAssemblyTypes()
        {

            if (TypesCache == null)
            {
                TypesCache = new Dictionary<string, Type>();

                foreach (var item in GetAllDynamicAssemblyNames())
                {
                    var assemblyTypes = GetDynamicTypes(item);

                    foreach (var type in assemblyTypes)
                    {
                        TypesCache.Add(type.Name, type);
                    }
                }
            }

            return TypesCache.Values.ToList();

        }

        /// <summary>
        /// Get Type from generated assemblies
        /// </summary>
        /// <param name="TypeName">string type name</param>
        /// <returns></returns>
        public static Type GetType(string TypeName)
        {
            if (TypesCache == null)
            {
                GetAllDynamicAssemblyTypes();
            }

            return TypesCache[TypeName];
        }

    }
}
