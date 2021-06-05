open System.Collections.Generic
open MongoDB.Bson
open MongoDB.Driver

// Easy way to create a KeyValuePair: "age" => 42
let inline (=>) key (value: obj) = KeyValuePair (key, value)
// Operator for applying C# implicit type conversions
// Several mongoDb methods leverage this implicit conversions but F# don't use them automaticallly
let inline (!>) (x:^a) : ^b = ((^a or ^b) : (static member op_Implicit : ^a -> ^b) x)
// Quick way to create a BsonDocument with just one key value pair
let inline bsonDoc key value = BsonDocument (BsonElement (key, !> value))
let inline bsonField name = StringFieldDefinition<_> (name)
let inline bsonFieldVal name = StringFieldDefinition<_,_> (name)

[< EntryPoint >]
let main _ =
    async {
        // Make a connection
        let client = MongoClient ()
        // Get a database
        let database = client.GetDatabase "foo"
        // Get a collection
        let collection = database.GetCollection "bar"

        // Insert a document
        let document = 
            BsonDocument ( [|
                "name"  => "MongoDB"
                "type"  => "Database"
                "count" => 1
                "info"  => BsonDocument ( [| "x" => 203; "y" => 102 |] ) |] ) 

        do! collection.InsertOneAsync document |> Async.AwaitTask

        // Insert multiple documents
        // generate 100 documents with a counter ranging from 0 - 99
        let documents = { 0 .. 99 } |> Seq.map (bsonDoc "counter") 
        do! collection.InsertManyAsync documents |> Async.AwaitTask

        // Counting documents
        let! count = collection.CountDocumentsAsync FilterDefinition.Empty |> Async.AwaitTask
        printf "%d" count

        // Query the collection

        // Find the first document in a collection
        let! firstDocument = collection.Find(FilterDefinition.Empty).FirstOrDefaultAsync () |> Async.AwaitTask
        printfn "%O" firstDocument

        // Find all documents in a collection
        let! allDocuments = collection.Find(FilterDefinition.Empty).ToListAsync () |> Async.AwaitTask
        allDocuments |> Seq.iter (printfn "%O") 
        
        do! collection.Find(FilterDefinition.Empty).ForEachAsync (printfn "%O") |> Async.AwaitTask

        let! cursor = collection.Find(FilterDefinition.Empty).ToCursorAsync () |> Async.AwaitTask
        for doc in cursor.ToEnumerable () do
            printfn "%O" doc

        // Get a single document with a filter
        let filter = Builders.Filter.Eq (bsonFieldVal "counter", 71)
        let! doc71 = collection.Find(filter).FirstAsync () |> Async.AwaitTask
        printfn "%O" doc71

        // Get a set of documents with a filter
        let filter50 = Builders.Filter.Gt (bsonFieldVal "counter", 50)
        let! cursor50 = collection.Find(filter50).ToCursorAsync () |> Async.AwaitTask
        for doc50 in cursor50.ToEnumerable () do
            printfn "%O" doc50

        do! collection.Find(filter50).ForEachAsync (printfn "%O") |> Async.AwaitTask

        // Get a range: 50 < counter <= 100
        let filterBuilder = Builders.Filter
        let filterRange = filterBuilder.And (
                            filterBuilder.Gt (bsonFieldVal "counter", 50),
                            filterBuilder.Lte (bsonFieldVal "counter", 100) )
        let! cursorRange = collection.Find(filterRange).ToCursorAsync () |> Async.AwaitTask
        for docRange in cursorRange.ToEnumerable () do
            printfn "%O" docRange

        do! collection.Find(filterRange).ForEachAsync (printfn "%O") |> Async.AwaitTask

        // Sorting documents
        let filterSort = Builders.Filter.Exists (bsonField "counter")
        let sort = Builders.Sort.Descending (bsonField "counter")
        
        let! docSort = collection.Find(filterSort).Sort(sort).FirstAsync () |> Async.AwaitTask
        printfn "%O" docSort

        // Projecting fields
        // The !> operator allows us to use the ProjectionDefinition C# implicit convertion
        // from ProjectionDefinition<BsonDocument> to ProjectionDefinition<BsonDocument,BsonDocument>
        // as required by the Project() method
        let projection = Builders.Projection.Exclude (bsonField "_id")
        let! docProjection = collection.Find(FilterDefinition.Empty).Project<_,_>(!> projection).FirstAsync () |> Async.AwaitTask
        printfn "%O" docProjection

        // Updating documents
        let filterUpdate = Builders.Filter.Eq (bsonFieldVal "counter", 10)
        let update = Builders.Update.Set (bsonFieldVal "counter", 110)

        let! updateResult = collection.UpdateOneAsync (filterUpdate, update) |> Async.AwaitTask
        printfn "%d" updateResult.ModifiedCount

        let filterMany = Builders.Filter.Lt (bsonFieldVal "counter", 100)
        let updateMany = Builders.Update.Inc (bsonFieldVal "counter", 100)
        let! result = collection.UpdateManyAsync (filterMany, updateMany) |> Async.AwaitTask
        if result.IsModifiedCountAvailable then
            printfn "%d" result.ModifiedCount

        // Deleting documents
        let filterDelete = Builders.Filter.Eq (bsonFieldVal "counter", 110)
        let! _ = collection.DeleteOneAsync filterDelete |> Async.AwaitTask

        let filterDeleteMany = Builders.Filter.Gte (bsonFieldVal "counter", 100)
        let! resultDeleteMany = collection.DeleteManyAsync filterDeleteMany |> Async.AwaitTask
        printfn "%d" resultDeleteMany.DeletedCount

        let models = [|
            InsertOneModel (bsonDoc "_id" 1) :> WriteModel<_>
            upcast InsertOneModel (bsonDoc "_id" 2)
            upcast InsertOneModel (bsonDoc "_id" 3)
            upcast InsertOneModel (bsonDoc "_id" 4)
            upcast UpdateOneModel (!> (bsonDoc "_id" 1), Builders.Update.Set (bsonFieldVal "x", 2))
            upcast DeleteOneModel (!> (bsonDoc "_id" 3))
            upcast ReplaceOneModel (!> (bsonDoc "_id" 4), (bsonDoc "_id" 4).AddRange (bsonDoc "x" 4))  |]
        
        let! resultBulk = collection.BulkWriteAsync models |> Async.AwaitTask

        if resultBulk.IsModifiedCountAvailable then
            printfn "%d" resultBulk.ModifiedCount

        // let! resultBulkNoOrder = collection.BulkWriteAsync (models, BulkWriteOptions (IsOrdered = false )) |> Async.AwaitTask

        // if resultBulkNoOrder.IsModifiedCountAvailable then
        //     printfn "%d" resultBulkNoOrder.ModifiedCount
    } |> Async.RunSynchronously

    0
