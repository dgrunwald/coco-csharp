﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <author name="Daniel Grunwald"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections;
using System.Globalization;
using System.IO;

namespace at.jku.ssw.Coco
{
public abstract class AbstractParserGen
{
	// other Coco objects
	protected readonly Errors errors;
	protected readonly Buffer buffer;
	protected readonly Tab tab;
	
	protected StreamWriter gen; // generated parser source file
	protected FileStream fram;  // parser frame file
	
	public Position preamblePos = null;   //!< position of "using" definitions from attributed grammar
	public Position semDeclPos = null;    //!< position of global semantic declarations
	
	const char CR  = '\r';
	const char LF  = '\n';
	protected const int maxTerm = 3;		// sets of size <= maxTerm are enumerated
	
	protected AbstractParserGen(Parser parser)
	{
		this.errors = parser.errors;
		this.buffer = parser.scanner.buffer;
		this.tab = parser.tab;
	}
	
	public abstract void WriteParser();
	
	public virtual void PrintStatistics()
	{
	}
	
	protected void Indent (int n) {
		for (int i = 1; i <= n; i++) gen.Write('\t');
	}
	
	protected readonly ArrayList symSet = new ArrayList();
	
	protected void InitSets() {
		for (int i = 0; i < symSet.Count; i++) {
			BitArray s = (BitArray)symSet[i];
			int[] bitValues = new int[(s.Length + 31) / 32];
			s.CopyTo(bitValues, 0);
			gen.Write("\t\tnew BitArray(new int[] {");
			for (int j = 0; j < bitValues.Length; j++) {
				if (j > 0)
					gen.Write(", ");
				//gen.Write("0x" + bitValues[j].ToString("x8"));
				gen.Write(bitValues[j].ToString());
			}
			if (i == symSet.Count-1) gen.WriteLine("})"); else gen.WriteLine("}),");
		}
	}
	
	protected int NewCondSet (BitArray s) {
		for (int i = 1; i < symSet.Count; i++) // skip symSet[0] (reserved for union of SYNC sets)
			if (Sets.Equals(s, (BitArray)symSet[i])) return i;
		symSet.Add(s.Clone());
		return symSet.Count - 1;
	}
	
	protected void OpenGen() {
		try {
			string fn = Path.Combine
			(
				tab.outDir,
				(tab.prefixName == null ? "" : tab.prefixName) + "Parser.cs"
			);

			if (tab.makeBackup && File.Exists(fn)) File.Copy(fn, fn + ".bak", true);
			gen = new StreamWriter(new FileStream(fn, FileMode.Create)); /* pdt */
		} catch (IOException) {
			throw new FatalError("Cannot generate parser file");
		}
	}
	
	protected void CopySourcePart(Position pos, int indent) {
		if (tab.lineDirectives && pos != null && pos.lineNumber > 0) {
			if (indent == 0) gen.WriteLine();
			gen.WriteLine("#line " + pos.lineNumber + " " + Path.GetFileName(tab.srcName));
		}
		tab.CopySourcePart(gen, pos, indent);
		if (tab.lineDirectives && pos != null && pos.lineNumber > 0) {
			if (indent == 0) gen.WriteLine();
			else gen.Write(new string('\t', indent));
			gen.WriteLine("#line default");
		}
	}
	
	protected void CopyFramePart(string stop, bool doOutput) {
		bool ok = tab.CopyFramePart(fram, gen, stop, doOutput);
		if (!ok)
		{
			throw new FatalError("Incomplete or corrupt parser frame file");
		}
	}

	protected void CopyFramePart(string stop) {
		CopyFramePart(stop, true);
	}
}
}
