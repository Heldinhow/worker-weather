# FAK em vez de FOK, e experimento paralelo no blind

Trocamos FOK por FAK em todas as execuções. FOK exige fill completo ou cancela — no log de produção, ordens de 4 shares falhavam sistematicamente porque o book não tinha liquidez suficiente para o fill total. FAK preenche o que estiver disponível imediatamente e cancela o restante, garantindo que qualquer posição parcial num Contested NO seja capturada.

Quando o cache está null (startup frio ou falha de refresh), disparamos dois orders FAK em paralelo: um limit FAK a 0.99 e um `createAndPostMarketOrder` FAK. O objetivo é comparar empiricamente qual execution type entrega o melhor preço de fill. Após dados suficientes, a estratégia blind será consolidada numa única abordagem.

Removemos também o filtro `maxNoPrice` do sweep do book: num Contested NO (temperatura já refutada), qualquer preço abaixo de 1.00 é lucrativo e velocidade de execução supera otimização de preço.
