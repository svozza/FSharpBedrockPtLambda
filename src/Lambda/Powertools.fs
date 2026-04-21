module FSharpBedrockPtLambda.Powertools

open AWS.Lambda.Powertools.Logging
open AWS.Lambda.Powertools.Metrics
open AWS.Lambda.Powertools.Tracing

let withPowertools (operationName: string) (f: 'a -> Async<'b>) (input: 'a) : Async<'b> =
    async {
        Tracing.AddAnnotation("operation", operationName) |> ignore
        Logger.LogInformation($"Starting {operationName}")
        try
            let! result = f input
            Metrics.AddMetric("SuccessCount", 1.0, MetricUnit.Count)
            return result
        with ex ->
            Logger.LogError($"Error in {operationName}: {ex.Message}")
            Metrics.AddMetric("ErrorCount", 1.0, MetricUnit.Count)
            return raise ex
    }

let flushMetrics () = Metrics.Flush()
