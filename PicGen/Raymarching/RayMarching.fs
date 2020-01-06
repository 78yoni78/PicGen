﻿module PicGen.Raymarching.Scenes

open PicGen
open PicGen.Raymarching
open Vector

type [<ReferenceEquality>] sceneObject =
    {   Color:vector3->color
        Reflection:float
        Roughness:float }
module sceneObject =
    let singleColor color reflection roughness = 
        {Color=(fun _ -> color); Reflection=reflection; Roughness=roughness}
    let checkerBoard color1 color2 reflection roughness = 
        let f p = 
            let sum = int (p |> map round |> reduce (+))
            if sum % 2 = 0 then color1
            else                color2
        {Color=f; Reflection=reflection; Roughness=roughness}

type field3d = field<vector3,sceneObject>

type renderConfig =
    {   //  Lighting 
        Reflections:int
        LightPosition:vector3
        Skybox:vector3->color
        SampleDistance:float
        //  RayMarching
        RayIterations:int
        RayDirection:vector3
        RayOrigin:vector3
        SurfaceDistance:float
        MaxRayDistance:float }

let raymarch ({RayOrigin=ro; RayDirection=rd; SurfaceDistance=surface; RayIterations=i; MaxRayDistance=maxDistance} as config:renderConfig) (scene:field<vector3,sceneObject>) = Field.raymarch ro rd surface i maxDistance scene

let color ({Reflections=reflections
            LightPosition=lightPos
            Skybox=skybox
            SampleDistance=sampleDistance

            RayDirection=rd
            RayOrigin=ro
            SurfaceDistance=surface
             } as config) field =
    match raymarch config field with
    | Ok (distance,({Color=colorMap; Reflection=reflective; Roughness=roughness} as object),iterations) ->
        let p, normal =
            let p' = ro .+. distance*.rd
            let normal = Field.normal sampleDistance p' field
            p' .+. 2.*.normal.*surface, normal
        let color = colorMap p
        let lightDir, lightDis =
            let u = lightPos .-. p
            let m = mag u
            u./m, m
        let colorWithLight = 
            let roughLight = (-.rd) *** normal
            let smoothLight = (reflect normal rd) *** lightDir
            let light = smoothLight*(1.-roughness) + roughLight*roughness   //  Lerps through smoothLight and roughLight by roughness
            color.*light
        colorWithLight
    | Error _ -> skybox rd
let frag ({RayDirection = rd} as config) scene (uv:vector2) = 
    let uv' = uv.*2. .- 1.
    let right = normalize ({X=0.;Y=1.;Z=0.} +++ rd)
    let up = normalize (rd +++ right)
    let rd = normalize (rd .+. right.*uv'.X .+. up.*uv'.Y)
    color {config with RayDirection=rd} scene