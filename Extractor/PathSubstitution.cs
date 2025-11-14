using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckLib.Models;
using TruckLib.Sii;

namespace Extractor
{
    internal class PathSubstitution
    {
        internal static (bool Modified, byte[] Buffer) SubstitutePathsInTextFormats(byte[] buffer,
            Dictionary<string, string> substitutions, string extension,
            Func<string, string, string> transformSubstitution = null,
            Action<string, string> onSubstitution = null)
        {
            var wasModified = false;

            var isSii = extension == ".sii";
            var isOtherTextFormat = extension == ".sui" || extension == ".mat";

            if (isSii)
            {
                buffer = SiiFile.Decode(buffer);
            }

            if (isSii || isOtherTextFormat)
            {
                var content = Encoding.UTF8.GetString(buffer);
                (content, wasModified) = TextUtils.ReplaceRenamedPaths(content, substitutions,
                    transformSubstitution, onSubstitution);

                buffer = Encoding.UTF8.GetBytes(content);
            }

            return (wasModified, buffer);
        }

        internal static (bool Modified, byte[] Buffer) SubstitutePathsInTobj(byte[] buffer,
            Dictionary<string, string> substitutions,
            Func<string, string, string> transformSubstitution = null,
            Action<string, string> onSubstitution = null)
        {
            var wasModified = false;

            var tobj = Tobj.Load(buffer);
            if (substitutions.TryGetValue(tobj.TexturePath, out var substitution))
            {
                var final = transformSubstitution?.Invoke(tobj.TexturePath, substitution) ?? substitution;
                tobj.TexturePath = final;
                wasModified = true;
                onSubstitution?.Invoke(tobj.TexturePath, final);
            }

            if (wasModified)
            {
                using var ms = new MemoryStream();
                using var w = new BinaryWriter(ms);
                tobj.Serialize(w);
                buffer = ms.ToArray();
            }

            return (wasModified, buffer);
        }
    }
}
