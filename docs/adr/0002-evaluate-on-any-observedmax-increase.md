# Avaliar buckets em qualquer aumento de ObservedMax dentro da Hot Window

O plano original definia `isCurrentHourObservation()` para bloquear `evaluateBuckets()` caso a observação não fosse da hora-alvo local da cidade.

Removemos o gate da hora do METAR dentro da Hot Window: sempre que ObservedMax aumentar durante a Hot Window, chamar `evaluateBuckets()` imediatamente — independentemente da hora da observação.

**Por que:** ObservedMax é monotonicamente crescente. Se uma observação dentro da Hot Window já prova que um bucket é impossível (`floor(convert(ObservedMax, bucket.unit)) > bucket.upperTemp`), o NO deve ser comprado naquele momento — não adiado até a próxima observação. O gate da hora do METAR só atrasaria compras corretas sem nenhum benefício de precisão.

Fora da Hot Window, warm poll continua atualizando ObservedMax e Dashboard, mas não posta Contested NO. Isso evita que o boot inicial transforme dados históricos do dia em uma rajada de ordens.
