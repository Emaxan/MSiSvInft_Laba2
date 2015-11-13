using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MSiSvInft_Laba2
{
    public class Analizator
    {
        public string Text;

        public Analizator(string text)
        {
            Text = text.ToLower();
            PrepareCode(ref Text);
        }

        private static void PrepareCode(ref string text)
        {
            var finder = new Regex("(--[^\n]*|(&\\s*|)(\"[^\"\n]*\"|ASCII\\.[\\w\\d_]+)(\\s*&|)|\'.\')");
            var match = finder.Match(text);
            while (match.Success)
            {
                text = text.Remove(match.Index, match.Length);
                match = finder.Match(text);
            }
            finder = new Regex(@"\b(procedure|function)(\s+)((\w|_)+)(\s*)(\([^\)]+\))(\s+)(return)?(\s*)([^;\s]+)(;)");
            match = finder.Match(text);
            while (match.Success)
            {
                text = text.Remove(match.Index, match.Length);
                match = finder.Match(text);
            }
        }

        public string Analiz()
        {
            var names = new SortedSet<string>();
            names.Clear();
            foreach (
                var temp in
                    from Match match in new Regex("(\\s*\\w[\\w\\d_]*\\s*)(,\\s*\\w[\\w\\d_]*\\s*)*:[^=]").Matches(Text)
                    select
                        match.Value.IndexOf(',') >= 0
                            ? match.Value.Substring(0, match.Value.Length - ": ".Length)
                                .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                            : new[] {match.Value.Substring(0, match.Value.Length - ": ".Length)})
            {
                for (var i = 0; i < temp.Count(); i++)
                    names.Add(temp[i].Trim());
            }
            if (names.Count < 1) return "Nothing";

            var nameArr = names.ToArray();

            var variablePoints = new Variable[names.Count];
            for (var i = 0; i < variablePoints.Count(); i++)
            {
                variablePoints[i].Name = nameArr[i];
                variablePoints[i].Points = new List<long>();
                var nameMatches = new Regex("\\b" + nameArr[i] + "\\b").Matches(Text);
                for (var j = 0; j < nameMatches.Count; j++)
                    variablePoints[i].Points.Add(nameMatches[j].Index);
            }

            var matchFunc = new Regex(@"(\bprocedure\b|\bfunction\b)\s*(\w*)\s*(\([^)]*\)|)\s*((return\s*\w+\s*|)?is)").Matches(Text);
            var func = (from Match match in matchFunc select FindFunc(Text, match)).ToArray();

            var list = func[0].Local.ToList();
            for (var n = 1; n < func.Count(); n++)
                for (var k = 0; k < func[n].Local.Count(); k++)
                    list.Remove(func[n].Local[k]);
            func[0].Local = list.ToArray();

            for (var i = 1; i < func.Count(); i++)
            {
                CountSpen(ref func[i], ref Text);
            }
            CountSpen(ref func[0], ref Text);
            return func.Aggregate(string.Empty,
                (current1, function) =>
                    function.Local.Aggregate(current1 + function.Type + ' ' + function.Name + ":\n",
                        (current, local) => current + ("\t" + local.Name + " - " + (local.Count - 1) + ";\n")));
        }

        private static void CountSpen(ref Function func, ref string text)
        {
            var declare = new Regex(@"(\bdeclare)((.|\n)+)(begin)((.|\n)+)(end;)").Matches(text, func.NamePos);
            foreach (Match declar in declare)
            {
                if (declar.Index >= func.EndPos) continue;
                foreach (Match localDec in new Regex("(\\s*\\w[\\w\\d_]*\\s*)(,\\s*\\w[\\w\\d_]*\\s*)*:[^=]").Matches(declar.Value))
                    if (localDec.Value.IndexOf(',') > 0)
                        foreach (
                            var local in
                                localDec.Value.Substring(0, localDec.Value.Length - ": ".Length).Split(','))
                        {
                            var count = 0;
                            var regex = new Regex(@"\b" + local.Trim() + @"\b");
                            var mat = regex.Match(text, declar.Index);
                            while (mat.Success && mat.Index < declar.Index + declar.Length)
                            {
                                count++;
                                var text1 = text.Remove(mat.Index);
                                var text2 = text.Remove(0, mat.Index + mat.Length);
                                text = text1;
                                for (var j = 0; j < mat.Length; j++) text += ' ';
                                text += text2;
                                mat = regex.Match(text, declar.Index);
                            }
                            func.Local[
                                func.Local.ToList()
                                    .IndexOf(new Local()
                                    {
                                        Count = 0,
                                        Name = local.Trim(),
                                        LocalType = LocalType.DeclareParametr
                                    })].Count = count - 1;
                        }
                    else
                    {
                        var count = 0;
                        var regex = new Regex(@"\b" + localDec.Value.Substring(0, localDec.Value.Length - ": ".Length).Trim() + @"\b");
                        var mat = regex.Match(text, declar.Index);
                        while (mat.Success && mat.Index < declar.Index + declar.Length)
                        {
                            count++;
                            var text1 = text.Remove(mat.Index);
                            var text2 = text.Remove(0, mat.Index + mat.Length);
                            text = text1;
                            for (var j = 0; j < mat.Length; j++) text += ' ';
                            text += text2;
                            mat = regex.Match(text, declar.Index);
                        }
                        func.Local[
                            func.Local.ToList()
                                .IndexOf(new Local()
                                {
                                    Count = 0,
                                    Name = localDec.Value.Substring(0, localDec.Value.Length - ": ".Length).Trim(),
                                    LocalType = LocalType.DeclareParametr
                                })].Count = count - 1;
                    }
            }



            for (var i = 0; i < func.Local.Length; i++)
            {
                if (func.Local[i].LocalType == LocalType.DeclareParametr) continue;
                var count = 0;
                var forstart = 0;
                var first = true;
                var reg = new Regex(@"\b" + func.Local[i].Name + @"\b");
                var match = reg.Match(text, func.NamePos);
                while (match.Success)
                {
                    if (func.Local[i].LocalType == LocalType.ForParametr && first)
                    {
                        forstart = match.Index;
                        first = false;
                    }
                    if (match.Index >
                        (func.Local[i].LocalType == LocalType.ForParametr
                            ? new Regex("end loop;").Match(text, forstart).Index
                            : func.EndPos)) break;
                    count++;
                    var text1 = text.Remove(match.Index);
                    var text2 = text.Remove(0, match.Index + match.Length);
                    text = text1;
                    for (var j = 0; j < match.Length; j++) text += ' ';
                    text += text2;
                    match = reg.Match(text, func.NamePos);
                }
                func.Local[i].Count = count;
            }
        }

        private static Function FindFunc(string text, Capture match)
        {
            var pos = match.Index + match.Length;
            var prototype = match.Value;
            var namePos = match.Index;
            var func = new Function {NamePos = namePos};
            var i = 0;
            while (prototype[i++] != ' ')
            {
            }
            func.Type = i == "function ".Length ? FuncType.Function : FuncType.Procedure;
            while (prototype[i++] == ' ')
            {
            }
            var j = i;
            while (prototype[i] != ' ')
            {
                i++;
            }
            while (!char.IsLetterOrDigit(prototype[i - 1])) i--;
            func.Name = prototype.Substring(j - 1, i - j + 1);
            func.EndPos = new Regex("\\bend\\s+" + func.Name + "\\b").Match(text, pos).Index;
            func.Local = GetLocals(text.Substring(func.NamePos, func.EndPos - func.NamePos)).ToArray();
            var declares = 
                new Regex(@"(\bdeclare)((.|\n)+)(begin)((.|\n)+)(end;)").Matches(text.Substring(func.NamePos,
                    func.EndPos - func.NamePos));
            foreach (Match declare in declares)
            {
                var localsInDeclare =
                    new Regex("(\\s*\\w[\\w\\d_]*\\s*)(,\\s*\\w[\\w\\d_]*\\s*)*:[^=]").Matches(declare.Value);
                foreach (Match localDec in localsInDeclare)
                    if (localDec.Value.IndexOf(',') > 0)
                        foreach (var local in localDec.Value.Substring(0, localDec.Value.Length - ": ".Length).Split(','))
                        {
                            for (var k = 0; k < func.Local.Length; k++)
                                if (func.Local[k].Name == local.Trim())
                                {
                                    func.Local[k].LocalType = LocalType.DeclareParametr;
                                    break;
                                }
                        }
                    else
                    {
                        for (var k = 0; k < func.Local.Length; k++)
                            if (func.Local[k].Name ==
                                localDec.Value.Substring(0, localDec.Value.Length - ": ".Length).Trim())
                            {
                                func.Local[k].LocalType = LocalType.DeclareParametr;
                                break;
                            }
                    }
            }
            return func;
        }

        private static IEnumerable<Local> GetLocals(string text)
        {
            var locals = new Regex("(\\s*\\w[\\w\\d_]*\\s*)(,\\s*\\w[\\w\\d_]*\\s*)*:[^=]").Matches(text);
            foreach (Match match in locals)
                if (match.Value.IndexOf(',') > 0)
                    foreach (var local in match.Value.Substring(0, match.Value.Length - ": ".Length).Split(','))
                        yield return new Local
                        {
                            Name = local.Trim(), 
                            Count = 0, 
                            LocalType = LocalType.GeneralType
                        };
                else
                    yield return new Local
                    {
                        Name = match.Value.Substring(0, match.Value.Length - ": ".Length).Trim(), 
                        Count = 0, 
                        LocalType = LocalType.GeneralType
                    };
            locals = new Regex(@"\bfor\s+[^\s]+\s+in\b").Matches(text);
            foreach (Match match in locals)
            {
                var space = match.Value.IndexOf(' ') + 1;
                var lastSpace = space;                   
                while (match.Value[lastSpace] != ' ') lastSpace++;
                yield return new Local
                {
                    Name = match.Value.Substring(space, lastSpace - space).Trim(), 
                    Count = 0, 
                    LocalType = LocalType.ForParametr
                };
            }
        }
    }

    struct Variable
    {
        public string Name;
        public List<long> Points;
    }

    public enum FuncType
    {
        Procedure,
        Function
    }

    struct Function
    {
        public string Name;
        public Local[] Local;
        public FuncType Type;
        public int NamePos, EndPos;
    }

    struct Local
    {
        public LocalType LocalType;
        public string Name;
        public long Count;
    }

    enum LocalType
    {
        ForParametr,
        DeclareParametr,
        GeneralType
    } 
}