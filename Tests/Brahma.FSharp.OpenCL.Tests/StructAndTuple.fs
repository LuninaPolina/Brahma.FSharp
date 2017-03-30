﻿module StructAndTuple

open NUnit.Framework
open System.IO
open System
open System.Reflection


open Brahma.Helpers
open OpenCL.Net
open Brahma.OpenCL
open Brahma.FSharp.OpenCL.Core
open System
open System.Reflection
open Microsoft.FSharp.Quotations
open Brahma.FSharp.OpenCL.Extensions



[<Struct>]
type a = 
        val mutable x: int 
        val mutable y: int     
        new (x1, y1) = {x = x1; y = y1}

[<Struct>]
type b = 
    val x: int 
    val mutable y: byte      
    new (x1, y1) = {x = x1; y = y1}

[<Struct>]
type c =
    val x: int 
    val y: int
    new (x1, y1) = {x = x1; y = y1} 

[<Struct>]
type d =
    val x: int 
    val y: int[]
    new (x1, y1) = {x = x1; y = y1}

[<TestFixture>]
type Translator() =
    let defaultInArrayLength = 4
    let intInArr = [|0..defaultInArrayLength-1|]
    let float32Arr = Array.init defaultInArrayLength (fun i -> float32 i)
    let _1d = new _1D(defaultInArrayLength, 1)
    let _2d = new _2D(defaultInArrayLength, 1)
    let deviceType = DeviceType.Default
    let platformName = "*"

    let provider =
        try  ComputeProvider.Create(platformName, deviceType)
        with
        | ex -> failwith ex.Message
 
    let checkResult command =
        let kernel,kernelPrepareF, kernelRunF = provider.Compile command    
        let commandQueue = new CommandQueue(provider, provider.Devices |> Seq.head)            
        let check (outArray:array<'a>) (expected:array<'a>) =        
            let cq = commandQueue.Add(kernelRunF()).Finish()
            let r = Array.zeroCreate expected.Length
            let cq2 = commandQueue.Add(outArray.ToHost(provider,r)).Finish()
            commandQueue.Dispose()
            Assert.AreEqual(expected, r)
            provider.CloseAllBuffers()
        kernelPrepareF,check
    
    [<Test>]
    member this.``truct int int``() = 
        let command = 
            <@ 
                fun (range:_1D) (buf:array<int>) (s:a)  -> 
                    buf.[0] <- s.x
            @>

        let s = new a(1, 1)
        let run1,check1 = checkResult command
        run1 _1d intInArr s      
        check1 intInArr [|1;1;2;3|]

    [<Test>]
    member this.``newstruct``() = 
        let command = 
            <@ 
                fun (range:_1D) (buf:array<int>)  -> 
                let s = new a(1, 2)
                buf.[0] <- s.x
            @>

        let run,check = checkResult command
        run _1d intInArr      
        check intInArr [|1;1;2;3|]

    [<Test>]
    member this.``change field``() = 
        let command = 
            <@ 
                fun (range:_1D) (buf:array<int>)  -> 
                let mutable s = new a(1, 2)
                s.x <- 6
                buf.[0] <- s.x
            @>

        let run,check = checkResult command
        run _1d intInArr      
        check intInArr [|6;1;2;3|]

    [<Test>]
    member this.``arr of structs``() = 
        let command = 
            <@ 
                fun(range:_1D) (buf:array<int>) (arr:array<a>) -> 
                    buf.[0] <- arr.[0].x
            
            @>
        let s1 = new a(2, 2)
        let s2 = new a(2, 2)
        let s3 = new a(2, 2)
        let run,check = checkResult command
        run _1d intInArr [|s1;s2;s3|]       
        check intInArr [|2;1;2;3|]

    [<Test>]
    member this.``Struct not mutable``() = 
        let command = 
            <@ 
                fun(range:_1D) (buf:array<int>) (s:c) -> 
                    buf.[0] <- s.x + s.y
            
            @>
        let s = new c(2, 3)
        let run,check = checkResult command
        run _1d intInArr s        
        check intInArr [|5;1;2;3|]

    [<Test>]
    member this.``Struct int byte``() = 
        let command = 
            <@ 
                fun(range:_1D) (buf:array<int>) (s:b) -> 
                    buf.[0] <- s.x
            
            @>
        let s = new b(1, 86uy)
        let run,check = checkResult command
        run _1d intInArr s        
        check intInArr [|1;1;2;3|]

    [<Test>]
    member this.``Struct with arr``() = //doesn't work
        let command = 
            <@ 
                fun(range:_1D) (buf:array<int>) (s:d) -> 
                    buf.[0] <- s.x
            
            @>
        let s = new d(1, [|1;2;3|])
        let run,check = checkResult command
        run _1d intInArr s        
        check intInArr [|1;1;2;3|]

    [<Test>]
    member this.``tuple``() = //doesn't work
        let command = 
            <@ 
                fun (range:_1D) (buf:array<int>) (s:int*int) -> 
                    buf.[0] <- 1
            @>
        let run,check = checkResult command
        run _1d intInArr (1,2)     
        check intInArr [|1;1;2;3|]