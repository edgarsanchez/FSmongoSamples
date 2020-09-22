# MongoDB .NET client driver samples in F#
This repo contains several code samples showing how to work with a mongoDB from a .NET application written in F#. The code is being compiled and run using .NET 5 (currently at RC1) and F# 5 but it should also work with .NET Core 3.1 and F# 4.7. So far I have got:
* The [MongoDB Driver Quick Tour](https://mongodb.github.io/mongo-csharp-driver/2.10/getting_started/quick_tour/) in the src/QuickTour folder
  * This sample shows how to make CRUD operations on a collection using BSON documents rather than classes.
  * You need to setup a local mongoDB server for this sample.
  * To run the sample get into the folder and enter `dotnet run`
* @KenWalger [CRUD C# Quick Start](https://www.mongodb.com/blog/post/quick-start-c-sharp-and-mongodb-starting-and-setup) in the src/QuickStartAlger folder
  * The sample comes from a blog series by Ken that shows how to do insert-read-update-delete operations on a MongoDB collection using the .NET MongoDB driver. The full C# code is [here](https://gist.github.com/kenwalger/f5cf317aa85aad2aa0f9d627d7a8095c)
  * You need to setup a free MongoDB Atlas cluster with the sample_training database, detailed instructions [here](https://www.mongodb.com/meetatlas) 
  * To run the sample get into the folder and enter `dotnet run`

Your comments and suggestions are welcomed!


