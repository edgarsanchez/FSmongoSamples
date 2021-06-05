open System
open FSharp.Control.Tasks //.V2.ContextInsensitive
open MongoDB.Bson
open MongoDB.Bson.Serialization.Attributes
open MongoDB.Driver

// Use this definition if you want to work with records
// type Product = {
//     [< BsonId >]                    mutable Id: ObjectId
//     [< BsonElement "SKU" >]         mutable SKU: int
//     [< BsonElement "Description" >] mutable Description: string
//     [< BsonElement "Price" >]       mutable Price: float
// }
// Use this definition if you want to work with classes
type Product (sku: int, description: string, price: float) =
    [< BsonId >] 
    member val Id = ObjectId.Empty with get, set
    [< BsonElement "SKU" >]
    member val SKU = sku with get, set
    [< BsonElement "Description" >]
    member val Description = description with get, set
    [< BsonElement "Price" >]
    member val Price = price with get, set

let mongoDBConnectionString = "mongodb+srv://<<YOUR-ATLAS-USER>>:<<PASSWORD>>@<<YOUR-CLUSTER>>.mongodb.net?retryWrites=true&w=majority"

let updateProductsAsync () =
    // Create client connection to our MongoDB database
    let client = MongoClient (mongoDBConnectionString)

    // Create the collection object that represents the "products" collection
    let database = client.GetDatabase "MongoDBStore"
    let products = database.GetCollection "products"

    task {
        // Clean up the collection if there is data in there
        do! database.DropCollectionAsync "products"

        // collections can't be created inside a transaction so create it first
        do! database.CreateCollectionAsync "products"

        // Create a session object that is used when leveraging transactions
        use! session = client.StartSessionAsync ()
        
        // Begin transaction
        session.StartTransaction ()

        try
            // Create some sample data. Use this three lines if you are working with records
            // let tv = { Id =  ObjectId.Empty; Description = "Television"; SKU = 4001; Price = 2000. }
            // let book = { Id =  ObjectId.Empty; Description = "A funny book"; SKU = 43221; Price = 19.99 }
            // let dogBowl = { Id =  ObjectId.Empty; Description = "Bowl for Fido"; SKU = 123; Price = 40.00 }
            // Create some sample data. Use this three lines if you are working with classes
            let tv = Product (description = "Television", sku = 4001, price = 2000.)
            let book = Product (description = "A funny book", sku = 43221, price = 19.99)
            let dogBowl = Product (description = "Bowl for Fido", sku = 123, price = 40.00)

            // Insert the sample data 
            do! products.InsertOneAsync (session, tv)
            do! products.InsertOneAsync (session, book)
            do! products.InsertOneAsync (session, dogBowl)

            let! resultsBeforeUpdates = products.Find(session, Builders.Filter.Empty).ToListAsync ()
            printfn "Original Prices:\n"
            for d in resultsBeforeUpdates do
                printfn "Product Name: %s\tPrice: %.2f" d.Description d.Price

            // Increase all the prices by 10% for all products
            let update = UpdateDefinitionBuilder<Product>().Mul ((fun r -> r.Price), 1.1)
            let! _ = products.UpdateManyAsync (session, Builders.Filter.Empty, update) //,options)

            // Made it here without error? Let's commit the transaction
            do! session.CommitTransactionAsync ()

            // Let's print the new results to the console
            printfn "\n\nNew Prices (10%% increase):\n"
            let! resultsAfterCommit = products.Find(session, Builders.Filter.Empty).ToListAsync ()
            for d in resultsAfterCommit do
                printfn "Product Name: %s\tPrice: %.2f" d.Description d.Price

            return true
        with
            e ->
                printfn "Error writing to MongoDB: %s" e.Message
                do! session.AbortTransactionAsync ()
                return false
    }

[< EntryPoint >]
let main _ =
    if updateProductsAsync().Result then
        printfn "Finished updating the product collection"
        Console.ReadKey () |> ignore
        0
    else
        1
