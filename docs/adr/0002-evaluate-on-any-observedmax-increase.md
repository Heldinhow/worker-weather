# Avaliar buckets em qualquer aumento de ObservedMax, sem gate de hora

O plano original definia `isCurrentHourObservation()` para bloquear `evaluateBuckets()` caso a observação não fosse da hora-alvo (ex: janela do 14h BRT só aceitaria obs de 17h UTC).

Removemos esse gate. A regra é: sempre que ObservedMax aumentar, chamar `evaluateBuckets()` imediatamente — independentemente da hora da observação.

**Por que:** ObservedMax é monotonicamente crescente. Se uma observação do 16h UTC já prova que um bucket é impossível (floor(28°C) > 26°C), o NO deve ser comprado naquele momento — não adiado até a obs do 17h UTC chegar. O gate de hora só atrasaria compras corretas sem nenhum benefício de precisão.
