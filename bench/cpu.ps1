$app = (Get-Process backend).TotalProcessorTime.TotalMilliseconds
$pg = ((Get-Counter "\Process(postgres*)\% Processor Time").CounterSamples | Measure-Object RawValue -Sum).Sum / 10000
"$app $pg"
