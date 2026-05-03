#!/usr/bin/env node

/**
 * Phycock Progress Sync
 * セッション終了時に My-Skill-Graph に進捗を同期
 *
 * 実行方法: node phycock-progress-sync.mjs
 * (設定: Stop hook で自動実行)
 */

import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { execSync } from 'child_process';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

// 定数
const SKILL_GRAPH_ROOT = path.normalize('C:/Users/harne/iCloudDrive/My-Skill-Graph/My-Skill-Graph');
const GOALS_FILE = path.join(SKILL_GRAPH_ROOT, 'self/goals.md');
const DECISIONS_DIR = path.join(SKILL_GRAPH_ROOT, 'decisions');

/**
 * My-Skill-Graph への同期メッセージを出力
 */
function syncProgressNotice() {
  const message = {
    systemMessage: `\n📚 進捗同期準備完了\n\nセッション終了時に以下を確認してください：\n1. My-Skill-Graph: ${GOALS_FILE}\n2. 完了事項を goals.md のアクティブスレッドから移動\n3. 新規設計判断があれば decisions/ に登録\n\n詳細: CLAUDE.md の「進捗同期」セクションを参照`
  };

  console.log(JSON.stringify(message));
}

/**
 * メイン処理
 */
function main() {
  try {
    // My-Skill-Graph が存在するか確認
    if (!fs.existsSync(GOALS_FILE)) {
      console.error(JSON.stringify({
        systemMessage: `⚠️ My-Skill-Graph が見つかりません\nパス: ${GOALS_FILE}`
      }));
      process.exit(1);
    }

    // 同期メッセージを出力
    syncProgressNotice();
    process.exit(0);

  } catch (error) {
    console.error(JSON.stringify({
      systemMessage: `❌ 同期エラー: ${error.message}`
    }));
    process.exit(1);
  }
}

main();
