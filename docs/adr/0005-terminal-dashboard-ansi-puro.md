# Terminal dashboard: ANSI puro com coordenação via logger

O bot precisa de uma visão live no terminal do estado por cidade (ObservedMax, timestamps, Hot Window) sem adicionar latência ao hot path. Escolhemos ANSI cursor manipulation puro em vez de uma biblioteca TUI (ink, blessed) porque o dashboard é um bloco fixo de N linhas sem elementos interativos — uma biblioteca adicionaria dependência e um modelo de rendering que não compra nada aqui.

Um módulo singleton `dashboard.ts` mantém o estado por cidade num `Map`; o hot path faz apenas `updateCity()` (Map.set, O(1), sem I/O); um `setInterval` de 1s lê esse Map e redesenha o bloco — nunca bloqueia o hot path.

## Coordenação com o logger

Eventos críticos (detecção de nova ObservedMax, trades, erros) compartilham stdout com o dashboard. Para evitar corrupção visual, `log()` apaga o bloco do dashboard antes de escrever e redesenha após. Dividir eventos críticos para stderr foi rejeitado porque excluiria trades de arquivos de log capturados via redirecionamento de stdout.

## Log file

`logger.ts` escreve texto puro (sem ANSI) num arquivo configurado via `LOG_FILE` env var. Sem valor padrão — ausência da var desativa o arquivo silenciosamente. Sem rotação no código; rotação externa via `logrotate` é responsabilidade do operador.
