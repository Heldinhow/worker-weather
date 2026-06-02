# Promise.all + .then() em vez de Promise.race para o hot path

O plano original propunha `Promise.race([noaa, aw])` para processar o vencedor imediatamente e depois aguardar o perdedor. Adotamos `Promise.all` com callbacks `.then()` inline em cada fetcher: cada resultado é processado no instante em que chega, e o loop só avança para o próximo round quando ambos terminam.

**Por que:** rastrear "qual foi o perdedor" após um `Promise.race` exige bookkeeping manual propenso a erro (closures, referências de promise). O padrão `.then()` entrega o mesmo comportamento — processamento imediato ao resolver — sem nenhuma coordenação extra. O throttle natural é a round-trip do fetcher mais lento, o que é correto: não faz sentido lançar novo round antes de ambos completarem.
