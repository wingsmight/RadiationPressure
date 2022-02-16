using System.Collections.Generic;
using UnityEngine;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine.UI;

namespace Battlehub.CodeAnalysis
{
    public interface ICompiler
    {
        byte[] Compile(string[] text, string[] references = null);
    }

#if !UNITY_WEBGL
    public class Complier : ICompiler
    {
        public byte[] Compile(string[] text, string[] extraReferences = null)
        {
            List<Microsoft.CodeAnalysis.SyntaxTree> syntaxTrees = new List<Microsoft.CodeAnalysis.SyntaxTree>();
            for(int i = 0; i < text.Length; ++i)
            {
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(text[i]));
            }
            
            string assemblyName = Path.GetRandomFileName();
            HashSet<string> references = new HashSet<string>
            {
                typeof(object).Assembly.Location,
                typeof(Enumerable).Assembly.Location,
                typeof(Object).Assembly.Location,
                typeof(Input).Assembly.Location,
                typeof(Rigidbody).Assembly.Location,
                typeof(AudioSource).Assembly.Location,
                typeof(Animator).Assembly.Location,
                typeof(ParticleSystem).Assembly.Location,
                typeof(Text).Assembly.Location,
                typeof(Canvas).Assembly.Location,                
                Assembly.GetCallingAssembly().Location
            };

            if(extraReferences != null)
            {
                for (int i = 0; i < extraReferences.Length; ++i)
                {
                    if (!references.Contains(extraReferences[i]))
                    {
                        references.Add(extraReferences[i]);
                    }
                }
            }

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: syntaxTrees,
                references: references.Select(r => MetadataReference.CreateFromFile(r)),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                    {
                        Debug.LogErrorFormat("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }
                    return null;
                }

                ms.Seek(0, SeekOrigin.Begin);
                return ms.ToArray();
            }
        }
    }
#endif
}

