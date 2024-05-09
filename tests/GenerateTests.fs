namespace Ionide.LanguageServerProtocol



module String =
  open System
  let toPascalCase (s: string) =
    s.[0]
    |> Char.ToUpper
    |> fun c -> c.ToString() + s.Substring(1)


module rec MetaModel =
  open System
  open Newtonsoft.Json.Linq
  open Newtonsoft.Json
  open Newtonsoft.Json.Converters

  let metaModel = IO.Path.Join(__SOURCE_DIRECTORY__, "..", "data", "3.17.0", "metaModel.json")
  let metaModelSchema = IO.Path.Join(__SOURCE_DIRECTORY__, "..", "data", "3.17.0", "metaModel.schema.json")
  type MetaData = { Version: string }
  type Requests = { Method: string }


  [<RequireQualifiedAccess>]
  type BaseTypes =
    | Uri
    | DocumentUri
    | Integer
    | Uinteger
    | Decimal
    | RegExp
    | String
    | Boolean
    | Null

    static member Parse(s: string) =
      match s with
      | "URI" -> Uri
      | "DocumentUri" -> DocumentUri
      | "integer" -> Integer
      | "uinteger" -> Uinteger
      | "decimal" -> Decimal
      | "RegExp" -> RegExp
      | "string" -> String
      | "boolean" -> Boolean
      | "null" -> Null
      | _ -> failwithf "Unknown base type: %s" s

    member x.ToDotNetType() =
      match x with
      | Uri -> "URI"
      | DocumentUri -> "DocumentUri"
      | Integer -> "int32"
      | Uinteger -> "uint32"
      | Decimal -> "decimal"
      | RegExp -> "RegExp"
      | String -> "string"
      | Boolean -> "bool"
      | Null -> "null"

  [<Literal>]
  let BaseTypeConst = "base"

  type BaseType = { Kind: string; Name: BaseTypes }

  [<Literal>]
  let ReferenceTypeConst = "reference"

  type ReferenceType = { Kind: string; Name: string }

  [<Literal>]
  let ArrayTypeConst = "array"

  type ArrayType = { Kind: string; Element: Type }

  [<Literal>]
  let MapTypeConst = "map"

  type MapType = { Kind: string; Key: MapKeyType; Value: Type }

  [<RequireQualifiedAccess>]
  type MapKeyNameEnum =
    | Uri
    | DocumentUri
    | String
    | Integer

    static member Parse(s: string) =
      match s with
      | "URI" -> Uri
      | "DocumentUri" -> DocumentUri
      | "string" -> String
      | "integer" -> Integer
      | _ -> failwithf "Unknown map key name: %s" s

    member x.ToDotNetType() =
      match x with
      | Uri -> "URI"
      | DocumentUri -> "DocumentUri"
      | String -> "string"
      | Integer -> "int32"

  [<RequireQualifiedAccess>]
  type MapKeyType =
    | ReferenceType of ReferenceType
    | Base of {| Kind: string; Name: MapKeyNameEnum |}

  [<Literal>]
  let AndTypeConst = "and"

  type AndType = { Kind: string; Items: Type array }

  [<Literal>]
  let OrTypeConst = "or"

  type OrType = { Kind: string; Items: Type array }

  [<Literal>]
  let TupleTypeConst = "tuple"

  type TupleType = { Kind: string; Items: Type array }

  type Property = {
    Deprecated: string option
    Documentation: string option
    Name: string
    Optional: bool option
    Proposed: bool option
    Required: bool option
    Since: string option
    Type: Type
  } with

    member x.IsOptional =
      x.Optional
      |> Option.defaultValue false

    member x.NameAsPascalCase =
      String.toPascalCase x.Name

  [<Literal>]
  let StructureTypeLiteral = "literal"

  type StructureLiteral = {
    Deprecated: string option
    Documentation: string option
    Properties: Property array
    Proposed: bool option
    Since: string option
  }

  type StructureLiteralType = { Kind: string; Value: StructureLiteral }

  [<Literal>]
  let StringLiteralTypeConst = "stringLiteral"

  type StringLiteralType = { Kind: string; Value: string }

  [<Literal>]
  let IntegerLiteralTypeConst = "integerLiteral"

  type IntegerLiteralType = { Kind: string; Value: decimal }

  [<Literal>]
  let BooleanLiteralTypeConst = "booleanLiteral"

  type BooleanLiteralType = { Kind: string; Value: bool }

  [<RequireQualifiedAccess>]
  type Type =
    | BaseType of BaseType
    | ReferenceType of ReferenceType
    | ArrayType of ArrayType
    | MapType of MapType
    | AndType of AndType
    | OrType of OrType
    | TupleType of TupleType
    | StructureLiteralType of StructureLiteralType
    | StringLiteralType of StringLiteralType
    | IntegerLiteralType of IntegerLiteralType
    | BooleanLiteralType of BooleanLiteralType


  type Structure = {
    Deprecated: string option
    Documentation: string option
    Extends: Type array
    Mixins: Type array
    Name: string
    Properties: Property array
    Proposed: bool option
    Since: string option
  }

  type TypeAlias = {
    Deprecated: string option
    Documentation: string option
    Name: string
    Proposed: bool option
    Since: string option
    Type: Type
  }
  
   [<JsonConverter(typeof<StringEnumConverter>)>]
  type EnumerationTypeNameValues =
  | String = 0
  | Integer = 1
  | Uinteger = 2

  type EnumerationType = {
    Kind : string
    Name : EnumerationTypeNameValues
  }

  type EnumerationEntry = {
    Deprecated: string option
    Documentation: string option
    Name: string
    Proposed: bool option
    Since: string option
    Value: string
  }

  type Enumeration = {
    Deprecated: string option
    Documentation: string option
    Name: string
    Proposed: bool option
    Since: string option
    SupportsCustomValues : bool option
    Type : EnumerationType
    Values: EnumerationEntry array
  }

  type MetaModel = {
    MetaData: MetaData
    Requests: Requests array
    Structures: Structure array
    TypeAliases: TypeAlias array
    Enumerations: Enumeration array
  }

  module Converters =

    type MapKeyTypeConverter() =
      inherit JsonConverter<MapKeyType>()

      override _.WriteJson(writer: JsonWriter, value: MapKeyType, serializer: JsonSerializer) : unit =
        failwith "Should never be writing this structure, it comes from Microsoft LSP Spec"

      override _.ReadJson
        (
          reader: JsonReader,
          objectType: System.Type,
          existingValue: MapKeyType,
          hasExistingValue,
          serializer: JsonSerializer
        ) =
        let jobj = JObject.Load(reader)
        let kind = jobj.["kind"].Value<string>()

        match kind with
        | ReferenceTypeConst ->
          let name = jobj.["name"].Value<string>()
          MapKeyType.ReferenceType { Kind = kind; Name = name }
        | "base" ->
          let name = jobj.["name"].Value<string>()
          MapKeyType.Base {| Kind = kind; Name = MapKeyNameEnum.Parse name |}
        | _ -> failwithf "Unknown map key type: %s" kind


    type TypeConverter() =
      inherit JsonConverter<Type>()

      override _.WriteJson(writer: JsonWriter, value: MetaModel.Type, serializer: JsonSerializer) : unit =
        failwith "Should never be writing this structure, it comes from Microsoft LSP Spec"

      override _.ReadJson
        (
          reader: JsonReader,
          objectType: System.Type,
          existingValue: Type,
          hasExistingValue,
          serializer: JsonSerializer
        ) =
        let jobj = JObject.Load(reader)
        let kind = jobj.["kind"].Value<string>()

        match kind with
        | BaseTypeConst ->
          let name = jobj.["name"].Value<string>()
          Type.BaseType { Kind = kind; Name = BaseTypes.Parse name }
        | ReferenceTypeConst ->
          let name = jobj.["name"].Value<string>()
          Type.ReferenceType { Kind = kind; Name = name }
        | ArrayTypeConst ->
          let element = jobj.["element"].ToObject<Type>(serializer)
          Type.ArrayType { Kind = kind; Element = element }
        | MapTypeConst ->
          let key = jobj.["key"].ToObject<MapKeyType>(serializer)
          let value = jobj.["value"].ToObject<Type>(serializer)
          Type.MapType { Kind = kind; Key = key; Value = value }
        | AndTypeConst ->
          let items = jobj.["items"].ToObject<Type[]>(serializer)
          Type.AndType { Kind = kind; Items = items }
        | OrTypeConst ->
          let items = jobj.["items"].ToObject<Type[]>(serializer)
          Type.OrType { Kind = kind; Items = items }
        | TupleTypeConst ->
          let items = jobj.["items"].ToObject<Type[]>(serializer)
          Type.TupleType { Kind = kind; Items = items }
        | StructureTypeLiteral ->
          let value = jobj.["value"].ToObject<StructureLiteral>(serializer)
          Type.StructureLiteralType { Kind = kind; Value = value }
        | StringLiteralTypeConst ->
          let value = jobj.["value"].Value<string>()
          Type.StringLiteralType { Kind = kind; Value = value }
        | IntegerLiteralTypeConst ->
          let value = jobj.["value"].Value<decimal>()
          Type.IntegerLiteralType { Kind = kind; Value = value }
        | BooleanLiteralTypeConst ->
          let value = jobj.["value"].Value<bool>()
          Type.BooleanLiteralType { Kind = kind; Value = value }
        | _ -> failwithf "Unknown type kind: %s" kind


  let metaModelSerializerSettings =
    let settings = JsonSerializerSettings()
    settings.Converters.Add(Converters.TypeConverter() :> JsonConverter)
    settings.Converters.Add(Converters.MapKeyTypeConverter() :> JsonConverter)
    settings.Converters.Add(JsonUtils.OptionConverter() :> JsonConverter)
    settings

module GenerateTests =

  let rangeZero = FSharp.Compiler.Text.Range.Zero

  open System
  open Expecto
  open Fantomas.Core

  open Fabulous.AST

  open type Fabulous.AST.Ast

  open System.IO
  open Newtonsoft.Json
  open Fantomas.Core.SyntaxOak

  let createOption (t: Type) = Type.AppPostfix(TypeAppPostFixNode(t, Type.FromString "option", rangeZero))

  let createGeneric name types =
    TypeAppPrefixNode(name, None, SingleTextNode("<", rangeZero), types, SingleTextNode(">", rangeZero), rangeZero)
    |> Type.AppPrefix

  let createAnonymousRecord types =
    TypeAnonRecordNode(None, Some(SingleTextNode("{|", rangeZero)), types, SingleTextNode("|}", rangeZero), rangeZero)
    |> Type.AnonRecord

  let createDictionary types = createGeneric (Type.FromString "System.Collections.Generic.Dictionary") types

  let createTuple (types: Type array) =
    let types =
      types
      // |> Array.toList
      |> Array.map (Choice1Of2)

    let types =
      types
      |> Array.collect (fun x -> 
        [|
          x
          Choice2Of2(SingleTextNode("*", rangeZero))
        |]
        
        )
    let types =
      types
      |> Array.removeAt (types.Length - 1)
      |> List.ofArray

    TypeTupleNode(types, rangeZero)
    |> Type.Tuple

  let createErasedUnion (types: Type array) =
    if types.Length > 1 then
      let duType = Type.FromString $"U%d{types.Length}"
      createGeneric duType (Array.toList types)
    else
      types.[0]

  let isNullableType (t: MetaModel.Type) =
    match t with
    | MetaModel.Type.BaseType { Name = MetaModel.BaseTypes.Null } -> true
    | _ -> false

  let appendAugment augment s =
    match augment with
    | Some x -> sprintf "%s %s" s x
    | None -> s


  let rec createField (currentType: MetaModel.Type) (currentProperty: MetaModel.Property) =
    try


      let rec getType (currentType: MetaModel.Type) =
        match currentType with
        | MetaModel.Type.ReferenceType r ->
          match r.Name with
          | "LSPAny" -> Type.FromString "obj"
          | _ ->
            let name = r.Name

            Type.FromString name
        | MetaModel.Type.BaseType b ->
          let name = b.Name.ToDotNetType()

          Type.FromString name
        | MetaModel.Type.OrType o ->

          // TS types can have optional properties (myKey?: string)
          // and unions with null (string | null)
          // we need to handle both cases
          let isOptional, items =
            if
              currentProperty.IsOptional
              || Array.exists (isNullableType) o.Items
            then
              true,
              o.Items
              |> Array.filter (fun x -> not (isNullableType x))
            else
              false, o.Items

          let ts =
            items
            |> Array.map getType

          // if this is already marked as Optional in the schema, ignore the union case
          // as we'll wrap it in an option type near the end
          if
            isOptional
            && not currentProperty.IsOptional
          then
            createOption (createErasedUnion ts)
          else
            createErasedUnion ts


        | MetaModel.Type.ArrayType a ->

          TypeArrayNode(getType a.Element, 1, rangeZero)
          |> Type.Array
        | MetaModel.Type.StructureLiteralType l ->
          if
            l.Value.Properties
            |> Array.isEmpty
          then
            Type.FromString "obj"
          else
            let ts =
              l.Value.Properties
              |> Array.map (fun p ->
                createField p.Type p
                |> Tree.compile
              )
              |> Array.map (fun (t: FieldNode) -> t.Name.Value, t.Type)
              |> Array.toList

            createAnonymousRecord ts

        | MetaModel.Type.MapType m ->
          let key =
            match m.Key with
            | MetaModel.MapKeyType.Base b ->
              b.Name.ToDotNetType()
              |> Type.FromString
            | MetaModel.MapKeyType.ReferenceType r ->
              r.Name
              |> Type.FromString

          let value = getType m.Value

          createDictionary [
            key
            value
          ]

        | MetaModel.Type.StringLiteralType t -> Type.FromString "string"
        | MetaModel.Type.TupleType t ->

          let ts =
            t.Items
            |> Array.map getType

          

          createTuple ts

        | _ -> failwithf "todo Property %A" currentType

      let t = getType currentType
      let t = if currentProperty.IsOptional then createOption t else t
      Field(currentProperty.NameAsPascalCase, t)
    with e ->
      raise
      <| Exception(sprintf "createField on %A  " currentProperty, e)


  let createSafeStructure (structure: MetaModel.Structure) =
    let structure =
      if
        structure.Extends
        |> isNull
      then
        { structure with Extends = [||] }
      else
        structure

    let structure =
      if
        structure.Mixins
        |> isNull
      then
        { structure with Mixins = [||] }
      else
        structure

    let structure =
      if
        structure.Properties
        |> isNull
      then
        { structure with Properties = [||] }
      else
        structure

    structure

  let isUnitStructure (structure: MetaModel.Structure) =

    let isEmptyExtends =
      structure.Extends
      |> isNull
      || structure.Extends
         |> Array.isEmpty

    let isEmptyMixins =
      structure.Mixins
      |> isNull
      || structure.Mixins
         |> Array.isEmpty

    let isEmptyProperties =
      structure.Properties
      |> isNull
      || structure.Properties
         |> Array.isEmpty

    isEmptyExtends
    && isEmptyMixins
    && isEmptyProperties

  let createStructure (structure: MetaModel.Structure) (model: MetaModel.MetaModel) =
    let rec expandFields (structure: MetaModel.Structure) = [
      let structure = createSafeStructure structure

      // TODO create interfaces from extensions and implement them
      for e in structure.Extends do
        match e with
        | MetaModel.Type.ReferenceType r ->
          match
            model.Structures
            |> Array.tryFind (fun s -> s.Name = r.Name)
          with
          | Some s -> yield! expandFields s
          | None -> failwithf "Could not find structure %s" r.Name
        | _ -> failwithf "todo Extends %A" e

      // Mixins are inlined fields
      for m in structure.Mixins do
        match m with
        | MetaModel.Type.ReferenceType r ->
          match
            model.Structures
            |> Array.tryFind (fun s -> s.Name = r.Name)
          with
          | Some s ->
            for p in s.Properties do
              createField p.Type p
          | None -> failwithf "Could not find structure %s" r.Name
        | _ -> failwithf "todo Mixins %A" m

      for p in structure.Properties do
        createField p.Type p
    ]

    try

      Record structure.Name { yield! expandFields structure }
    with e ->
      raise
      <| Exception(sprintf "createStructure on %A" structure, e)

  let createTypeAlias (alias: MetaModel.TypeAlias) =
    let rec getType (t: MetaModel.Type) =
      if alias.Name = "LSPAny" then Type.FromString "obj" 
      else
        match t with
        | MetaModel.Type.ReferenceType r -> Type.FromString r.Name
        | MetaModel.Type.BaseType b -> Type.FromString(b.Name.ToDotNetType())
        | MetaModel.Type.OrType o ->
          let ts =
            o.Items
            |> Array.map getType

          createErasedUnion ts
        | MetaModel.Type.ArrayType a ->
          TypeArrayNode(getType a.Element, 1, rangeZero)
          |> Type.Array
        | MetaModel.Type.StructureLiteralType l ->
          if
            l.Value.Properties
            |> Array.isEmpty
          then
            Type.FromString "obj"
          else
            let ts =
              l.Value.Properties
              |> Array.map (fun p ->
                createField p.Type p
                |> Tree.compile
              )
              |> Array.map (fun (t: FieldNode) -> t.Name.Value, t.Type)
              |> Array.toList

            createAnonymousRecord ts

        | MetaModel.Type.MapType m ->
          let key =
            match m.Key with
            | MetaModel.MapKeyType.Base b ->
              b.Name.ToDotNetType()
              |> Type.FromString
            | MetaModel.MapKeyType.ReferenceType r ->
              r.Name
              |> Type.FromString

          let value = getType m.Value

          createDictionary [
            key
            value
          ]

        | MetaModel.Type.StringLiteralType t -> Type.FromString "string"
        | MetaModel.Type.TupleType t ->

          let ts =
            t.Items
            |> Array.map getType

          createTuple ts

        | _ -> failwithf "todo Property %A" t


    getType alias.Type

  let createEnumeration (enumeration: MetaModel.Enumeration) =
    match enumeration.Type.Name with
    | MetaModel.EnumerationTypeNameValues.String ->
      Enum enumeration.Name {
        for i,v in enumeration.Values |> Array.mapi(fun i x -> i,x) do
          // if v.Name.ToLower() <> v.Value.ToLower() then failwithf "Unknown string literal enum combo %A %A" v.Name v.Value
          EnumCase(String.toPascalCase v.Name, string i)
      }
    | MetaModel.EnumerationTypeNameValues.Integer | MetaModel.EnumerationTypeNameValues.Uinteger  ->
      Enum enumeration.Name {
        for v in enumeration.Values do
          EnumCase(String.toPascalCase v.Name, v.Value)
      }
    | _ -> failwithf "todo Enumeration %A" enumeration


  let generateTests =
    testList "Generations" [
      testCaseAsync "Can Parse MetaModel"
      <| async {
        let! metaModel =
          File.ReadAllTextAsync(MetaModel.metaModel)
          |> Async.AwaitTask

        let parsedMetaModel =
          JsonConvert.DeserializeObject<MetaModel.MetaModel>(metaModel, MetaModel.metaModelSerializerSettings)

        let createErasedUnionType i =
          let unionName = SingleTextNode($"U%d{i}", rangeZero)

          let createAttribute name =
            let attributeName =
              AttributeNode(
                (IdentListNode([ IdentifierOrDot.Ident(SingleTextNode(name, rangeZero)) ], rangeZero)),
                None,
                None,
                rangeZero
              )

            AttributeListNode(
              (SingleTextNode("[<", rangeZero)),
              [ attributeName ],
              (SingleTextNode(">]", rangeZero)),
              rangeZero
            )

          let nameNode =
            let attributes = MultipleAttributeListNode([ createAttribute "ErasedUnion" ], rangeZero)

            let typeParams =
              let decls = [
                for j = 1 to i do
                  TyparDeclNode(None, (SingleTextNode($"'T{j}", rangeZero)), rangeZero)
              ]

              TyparDeclsPostfixListNode(
                SingleTextNode("<", rangeZero),
                decls,
                [],
                SingleTextNode(">", rangeZero),
                rangeZero
              )
              |> TyparDecls.PostfixList

            TypeNameNode(
              None,
              (Some attributes),
              SingleTextNode("type", rangeZero),
              None,
              IdentListNode([ IdentifierOrDot.Ident(unionName) ], rangeZero),
              (Some typeParams),
              [],
              None,
              Some(SingleTextNode("=", rangeZero)),
              None,
              rangeZero
            )

          let cases = [
            for j = 1 to i do
              UnionCaseNode(
                None,
                None,
                (Some(SingleTextNode("|", rangeZero))),
                SingleTextNode($"C{j}", rangeZero),
                [ FieldNode(None, None, None, false, None, None, Type.FromString $"'T{j}", rangeZero) ],
                rangeZero
              )
          ]

          TypeDefnUnionNode(nameNode, None, cases, [], rangeZero)

        let source =
          Namespace("Ionide.LanguageServerProtocol.Types").isRecursive () {
            // Simple aliases for types that are not in dotnet
            Abbrev("URI", Type.FromString "string")
            Abbrev("DocumentUri", Type.FromString "string")
            Abbrev("RegExp", Type.FromString "string")

            // Assuming the max is 5, can be increased if needed
            for i in [ 2..5 ] do
              EscapeHatch(createErasedUnionType i)


            for s in parsedMetaModel.Structures do
              if isUnitStructure s then
                Abbrev(s.Name, Type.FromString "unit")
              else
                createStructure s parsedMetaModel


            for t in parsedMetaModel.TypeAliases do
              // printfn "%A" t.Name
              Abbrev(t.Name, createTypeAlias t)

            for e in parsedMetaModel.Enumerations do
              createEnumeration e



          }

        let writeToFile path contents = File.WriteAllText(path, contents)

        Tree.compile source
        |> CodeFormatter.FormatOakAsync
        |> Async.RunSynchronously
        |> writeToFile "test.fsx"

        ()
      }
    ]


  [<Tests>]
  let tests = ftestList "Generate" [ generateTests ]