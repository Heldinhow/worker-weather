import { ClobClient, Chain, SignatureTypeV2 } from "@polymarket/clob-client-v2";
import { Wallet } from "@ethersproject/wallet";
import type { Config } from "./config.ts";

const CLOB_HOST = "https://clob.polymarket.com";

export async function initClobClient(config: Config): Promise<ClobClient> {
  const wallet = new Wallet(config.privateKey);
  const sigType = config.signatureType !== undefined
    ? (Number(config.signatureType) as SignatureTypeV2)
    : SignatureTypeV2.EOA;
  const base = new ClobClient({
    host: CLOB_HOST,
    chain: Chain.POLYGON,
    signer: wallet as any,
    signatureType: sigType,
    funderAddress: config.funderAddress,
  });
  const creds = await base.createOrDeriveApiKey();
  return new ClobClient({
    host: CLOB_HOST,
    chain: Chain.POLYGON,
    signer: wallet as any,
    creds,
    signatureType: sigType,
    funderAddress: config.funderAddress,
  });
}
