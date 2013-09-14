﻿module Doc
open System
open System.Reflection
open System
open ClariusLabs.NuDoc 

type DocArgs =
    {
        Write : string -> unit
        WriteLine : string -> unit
    }
    static member Console () = { Write = Console.Write; WriteLine = Console.WriteLine }

//Markdown doc writer (NuDoc visitor).
type private MarkdownWriter(write, writeLine) =
    inherit Visitor()

    let writeBlankLine = writeLine ""

    member __.NormalizeLink((cref:string)) =
        cref.Replace(":", "-").Replace("(", "-").Replace(")", "");

    override __.VisitMember(m) = 
        writeLine (new string('-', 50))
        writeLine ("# " + m.Id)
        base.VisitMember(m)

    override __.VisitSummary(summary) =
        writeBlankLine
        writeLine "## Summary"
        base.VisitSummary(summary)

    override __.VisitRemarks(remarks) =
        writeBlankLine
        writeLine "## Remarks"
        base.VisitRemarks(remarks)

    override __.VisitExample(example) =
        writeBlankLine
        writeLine "### Example"
        base.VisitExample(example)

    override __.VisitClass(c) =
        writeLine (c.Info.Name.ToString())

    override __.VisitC(code) =
        // Wrap inline code in ` according to Markdown syntax.
        writeLine " `"
        write code.Content
        write "` "
        base.VisitC(code)

    override __.VisitCode(code) =
        writeBlankLine
        writeBlankLine

        let writeCodeLine (line:string) = 
            // Indent code with 4 spaces according to Markdown syntax.
            write "    "
            writeLine line 

        code.Content.Split([| Environment.NewLine |], StringSplitOptions.None)
        |> Array.iter writeCodeLine

        base.VisitCode(code)

    override __.VisitText(text) =
        //Commented out as duping summary.
        write(text.Content)
        base.VisitText(text)

    override __.VisitPara(para) =
        writeBlankLine
        writeBlankLine
        base.VisitPara(para)
        writeBlankLine
        writeBlankLine

    override __.VisitSee(see) =
        let cref = __.NormalizeLink(see.Cref)
        Console.Write(" [{0}]({1}) ", cref.Substring(2), cref)

    override __.VisitSeeAlso(seeAlso) =
        let cref = __.NormalizeLink(seeAlso.Cref)
        writeLine (sprintf "[%s](%s)" (cref.Substring(2)) cref)

//Active patterns for interesting types.
let (|Member|_|) (m:Element) = match m with | :? Member as m -> Some(m) | _ -> None 
let (|Class|_|) (x:Element) = match x with | :? Class as c -> Some(c) | _ -> None
let (|TypeDeclaration|_|) (x:Element) = match x with | :? TypeDeclaration as t -> Some(t) | _ -> None

///Document an assembly.
let docAssembly args path = 

    //Read from assembly so we get the reflection info.
    let ass = Assembly.LoadFrom(path)
    let members = DocReader.Read(ass)

    //Document a type (struct, calss, enum, interface).
    let docType (td:TypeDeclaration) = 

        let maybeMember = 
            let memberMap = 
                members.Elements
                |> Seq.choose (|Member|_|)
                |> Seq.map (fun m -> m.Id, m)
                |> Map.ofSeq
            fun id -> memberMap.TryFind(id)

        let tdIds = 
            let memberIdMap = MemberIdMap()
            //MethodInfo on TypeDeclaration is a Type.
            memberIdMap.Add(td.Info :?> Type)
            memberIdMap.Ids

        let writer = MarkdownWriter(args.Write, args.WriteLine) :> Visitor

        let visit (e:Element) = e.Accept(writer) |> ignore

        tdIds
        |> Seq.choose maybeMember
        |> Seq.map (fun m -> m.Elements)
        |> Seq.iter (fun elems -> elems |> Seq.iter visit)
    
    //Let's do this!
    members.Elements
    |> Seq.choose (|TypeDeclaration|_|)
    |> Seq.iter docType




(*
        //DON'T LOOK DOWN HERE! :) 
        //Look up a doc member from the assembly.
        let maybeMember = 
            let memberMap = 
                members.Elements
                |> Seq.choose (|Member|_|)
                |> Seq.map (fun m -> m.Id, m)
                |> Map.ofSeq
            fun id -> memberMap.TryFind(id)
        



        let traverseVisit (e:Element) =
            let elems = e.Traverse()
            elems |> Seq.iter visit 

        //Doc the type elements.
        //td.Accept(writer) |> ignore
        traverseVisit td
        visit td
        td.Elements |> Seq.iter visit

        let typeMemberIds = 
            let m = MemberIdMap()
            //MethodInfo on TypeDeclaration is a Type.
            m.Add(td.Info :?> Type)
            m.Ids

        //
        typeMemberIds
        |> Seq.choose maybeMember
        |> Seq.iter visit
*)