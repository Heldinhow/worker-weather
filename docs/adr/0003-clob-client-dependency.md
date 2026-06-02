# Uso de @polymarket/clob-client-v2 em vez de signing manual

O plano declarava "Bun built-ins only, no unnecessary dependencies." Para postagem de ordens L2 no Polymarket CLOB, relaxamos essa regra para duas dependências cirúrgicas: `@polymarket/clob-client-v2` e `@ethersproject/wallet`.

**Por que:** implementar assinatura L2 Polymarket do zero exige ECDSA + ABI encoding específico do protocolo — semanas de trabalho fora do escopo do bot. O `@polymarket/clob-client-v2` já é usado e validado em produção pelo projeto de referência (`polymarket-trade-engine`). Todas as outras dependências (HTTP framework, ORM, state management) permanecem proibidas.
