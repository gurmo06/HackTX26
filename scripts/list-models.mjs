import { GoogleGenAI } from '@google/genai';
if (!process.env.GEMINI_API_KEY) throw new Error('Set GEMINI_API_KEY privately in .env first.');
const ai = new GoogleGenAI({ apiKey: process.env.GEMINI_API_KEY });
for await (const model of await ai.models.list({ config: { pageSize: 100 } })) console.log(model.name);
