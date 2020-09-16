# MongoDB .NET client driver samples in F#
This repo will contain several code samples how to work with a mongoDB from a .NET application written in F#. The code is being compiled and run using .NET 5 (currently at RC1) and F# 5 but it should also work with .NET Core 3.1 and F# 4.7. So far I have got:
* The [MongoDB Driver Quick Tour](https://mongodb.github.io/mongo-csharp-driver/2.10/getting_started/quick_tour/) in the src/QuickTour folder
  * This sample shows how to make CRUD operations on a collection using BSON documents rather than classes.
  * To run the sample get into the folder and enter `dotnet run`

All the samples assume a local mongoDB server installed locally.

Your comments and suggestions welcomed!


