open System.Collections.Generic
open MongoDB.Bson
open MongoDB.Driver

// Easy way to create a KeyValuePair: "age" => 42
let inline (=>) key (value: obj) = KeyValuePair (key, value)
// Several mongoDb methods leverage C# implicit conversions but F# doesn't use them automaticallly
// So we use the !> operator to explicitly do the conversions when needed
let inline (!>) (x:^a) : ^b = ((^a or ^b) : (static member op_Implicit : ^a -> ^b) x)
// Quick way to create a BsonDocument with just one key value pair
let inline bsonDoc key value = BsonDocument (BsonElement (key, !> value))
// Some filter methods require a StringFieldDefinition<string> as first parameter
// bsonField allows to quickly create such parameter
let inline bsonField name = StringFieldDefinition<_> (name)
// Some filter methods require a StringFieldDefinition<string,'a> as first parameter, where 'a is the type of the second parameter
// bsonFieldVal allows to quickly create such parameter
let inline bsonFieldVal name = StringFieldDefinition<_,_> (name)

[< EntryPoint >]
let main _ =
    let dbClient = MongoClient ("mongodb+srv://<<YOUR-ATLAS-USER>>:<<PASSWORD>>@<<YOUR-CLUSTER>>.mongodb.net?retryWrites=true&w=majority")
    
    let dbList = dbClient.ListDatabases().ToEnumerable ()

    printfn "The list of databases on this server is:"
    dbList |> Seq.iter (printfn "%O")

    printfn "Connecting to sample_training.grades"

    let database = dbClient.GetDatabase "sample_training"
    let collection = database.GetCollection "grades"

    // Define a new student for the grade book.
    let document = BsonDocument [
        "student_id" => 10000
        "scores" => BsonArray [
            BsonDocument [ "type" => "exam"; "score" => 88.12334193287023 ]
            BsonDocument [ "type" => "quiz"; "score" => 74.92381029342834 ]
            BsonDocument [ "type" => "homework"; "score" => 89.97929384290324 ]
            BsonDocument [ "type" => "homework"; "score" => 82.12931030513218 ]
        ]
        "class_id" => 480
    ]

    // *********************************
    // Create Operations
    // *********************************

    // Insert the new student grade records into the database.

    printfn "Inserting Document"
    collection.InsertOne document
    printfn "Document Inserted.\n"

    // *********************************
    // Read Operations
    // *********************************

    // Find first record in the database
    let firstDocument = collection.Find(FilterDefinition.Empty).FirstOrDefault ()
    printfn "\n**********\n"
    printfn "%O" firstDocument

    // Find a specific document with a filter
    let filter = Builders.Filter.Eq (bsonFieldVal "student_id", 10000)
    let studentDocument = collection.Find(filter).FirstOrDefault ()
    printfn "\n**********\n"
    printfn "%O" studentDocument

    // Find all documents with an exam score equal or above 95 as a list
    // let highExamScoreFilter = 
    //     Builders.Filter.And (
    //         Builders.Filter.Eq(bsonFieldVal "scores.type", "exam"),
    //         Builders.Filter.Gte(bsonFieldVal "scores.score", 95) )
    let highExamScoreFilter = Builders.Filter.ElemMatch (
                                bsonField "scores",
                                !> (BsonDocument [ "type" => "exam"; "score" => bsonDoc "$gte" 95 ]) )
    let highExamScores = collection.Find(highExamScoreFilter).ToList ()
    printfn "\n**********\n"
    highExamScores |> Seq.iter (printfn "%O")

    // Find all documents with an exam score equal
    // or above 95 as an iterable

    let cursor = collection.Find(highExamScoreFilter).ToCursor ()
    printfn "\n**********\n"
    printfn "\nHigh Scores Iterable\n"
    printfn "\n**********\n"
    for cursorDocument in cursor.ToEnumerable () do
        printfn "%O" cursorDocument

    // Sort the exam scores by student_id
    let sort = Builders.Sort.Descending (bsonField "student_id")
    let highestScore = collection.Find(highExamScoreFilter).Sort(sort).First ()
    printfn "\n**********\n"
    printfn "\nHigh Score\n"
    printfn "\n**********\n"
    printfn "%O" highestScore


    // *********************************
    // Update Operations
    // *********************************

    // Update quiz score.
    printfn "\n**********\n"
    printfn "Update class_id"
    let quizUpdateFilter = Builders.Filter.Eq (bsonFieldVal "student_id", 10000)

    let update = Builders.Update.Set (bsonFieldVal "class_id", 483)
    let result = collection.UpdateOne (quizUpdateFilter, update)
    printfn "%O" result

    // Array Updates
    printfn "\n**********\n"
    printfn "Update score.type.quiz array value"
    let arrayFilter = Builders.Filter.And(
                        Builders.Filter.Eq (bsonFieldVal "student_id", 10000),
                        Builders.Filter.Eq (bsonFieldVal "scores.type", "quiz") )
    let arrayUpdate = Builders.Update.Set (bsonFieldVal "scores.$.score", 84.92381029342834)

    let arrayUpdateResult = collection.UpdateOne (arrayFilter, arrayUpdate)
    printfn "%O" arrayUpdateResult

    //// *********************************
    //// Delete Operations
    //// *********************************

    // Delete the student record.
    printfn "\n**********\n"
    printfn "Deleting the record."
    let deleteFilter = Builders.Filter.Eq (bsonFieldVal "student_id", 10000)
    let deleteResult = collection.DeleteOne deleteFilter
    printfn "%O" deleteResult

    // Delete the low exams.
    printfn "\n**********\n"
    printfn "Deleting the record."
    let deleteLowExamFilter = Builders.Filter.ElemMatch (
                                bsonField "scores",
                                !> (BsonDocument [ "type" => "exam"; "score" => bsonDoc "$lt" 60 ]) )

    let deleteManyResults = collection.DeleteMany deleteLowExamFilter
    printfn "%O" deleteManyResults

    0