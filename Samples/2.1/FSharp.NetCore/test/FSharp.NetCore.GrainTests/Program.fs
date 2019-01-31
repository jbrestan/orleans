open System
open FSharp.Control.Tasks
open Microsoft.Extensions.Logging
open Orleans
open Orleans.Hosting
open Orleans.TestingHost
open Xunit
open FSharp.NetCore.Grains
open FSharp.NetCore.Interfaces
open Microsoft.Extensions.Configuration
open Orleankka.Client
open Orleankka.Cluster


type ClientConfigurator () =
    interface IClientBuilderConfigurator with
        member this.Configure (configuration: IConfiguration, clientBuilder: IClientBuilder) =
            clientBuilder
                .ConfigureApplicationParts(fun appParts ->
                    appParts
                        .AddApplicationPart(typeof<IHello>.Assembly)
                        .WithCodeGeneration()
                    |> ignore)
                .UseOrleankka()
                |> ignore

type SiloConfigurator () =
    interface ISiloBuilderConfigurator with
        member this.Configure (siloBuilder: ISiloHostBuilder) =
            siloBuilder
                .UseInMemoryReminderService()
                .ConfigureApplicationParts(fun appParts ->
                    appParts
                        .AddApplicationPart(typeof<HelloGrain>.Assembly)
                        .AddApplicationPart(typeof<IHello>.Assembly)
                        .WithCodeGeneration()
                    |> ignore)
                .UseOrleankka()
                |> ignore

type Tests () =

    let builder = new TestClusterBuilder()
    do builder.AddClientBuilderConfigurator<ClientConfigurator>()
    do builder.AddSiloBuilderConfigurator<SiloConfigurator>()
    let silo = builder.Build()
    do silo.Deploy()

    [<Fact>]
    member this.``Hello grain greets back`` () =
        task {
            let c = silo.Client.GetGrain<IHello> 0L
            let! response = c.SayHello("Hi!")
            Assert.Equal("You said: Hi!, I say: Hello!", response)
        }

    interface IDisposable with
        member this.Dispose() = silo.StopAllSilos()
