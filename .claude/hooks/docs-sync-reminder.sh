#!/usr/bin/env bash
# PostToolUse hook — 코드분석 문서 동기화 리마인더 (CLAUDE.md R1)
#
# 목적: A2Z/Form1.*.cs 파일이 Edit/Write 될 때마다
#       docs/코드분석/ 갱신을 상기시킨다.
#
# 2026-08-22 개정
#   - 가리키는 곳을 docs/기능/·docs/code-reference/ → docs/코드분석/ 으로 변경.
#     옛 두 폴더는 폐기 예정이고, 이미 죽은 핸들러를 설명하고 있었다.
#   - "생략 가능합니다" 문구 삭제. 빠져나갈 구멍을 리마인더가 알려주고 있었고,
#     그 결과 문서 80%가 7월에 멈췄다.
#   - 자동생성/ 재실행 안내 추가 — 손으로 고칠 필요가 없는 부분이다.
#
# 한계: 이 훅은 Claude Code에만 걸린다. 사용자가 VS Code에서 직접 고치거나
#       Codex로 고치면 뜨지 않는다. 안전장치가 아니라 보조 장치다.
#
# 의존성: bash, cat, grep, sed, printf, tr (jq 불필요)
# 입력:   stdin으로 PostToolUse 이벤트 JSON (tool_input.file_path 포함)
# 출력:   매칭 시 stdout에 hookSpecificOutput JSON → additionalContext 주입
#         매칭 안되면 조용히 exit 0

set -u

input=$(cat)

# JSON에서 file_path 추출 (jq 없이)
file_path=$(printf '%s' "$input" \
  | grep -oE '"file_path"[[:space:]]*:[[:space:]]*"[^"]+"' \
  | head -1 \
  | sed -E 's/.*"file_path"[[:space:]]*:[[:space:]]*"([^"]+)".*/\1/')

# 경로가 비어있으면 종료
if [ -z "${file_path:-}" ]; then
  exit 0
fi

# Windows 경로의 백슬래시를 슬래시로 정규화
normalized=$(printf '%s' "$file_path" | tr '\\' '/')

# Form1.{뭐든}.cs 패턴 매칭
if printf '%s' "$normalized" | grep -qE 'Form1\.[A-Za-z]+\.cs$'; then
  cat <<'EOF'
{"hookSpecificOutput":{"hookEventName":"PostToolUse","additionalContext":"[docs-sync-reminder] 방금 A2Z/Form1.*.cs 를 수정했습니다. CLAUDE.md R1에 따라 docs/코드분석/ 을 맞추세요.\n\n1) 손으로 쓴 문서 — docs/코드분석/파일별/{수정한 파일}.md\n   실행 순서(2절)·상태(3절)·알고리즘(5절)이 바뀌었으면 갱신. 줄 번호도 함께.\n   계산 규칙 자체가 바뀌었으면 docs/코드분석/알고리즘/ 도 확인.\n\n2) 자동 생성 문서 — 손으로 고치지 말고 재실행:\n   python docs/코드분석/generate.py\n   (버튼별 코드 위치 / 함수 목록 / 파일 구조 3개가 다시 만들어집니다)\n\n3) 새로 발견한 죽은 코드·중복·하드코딩은 docs/코드분석/판정/ 에 적으세요.\n\n포매팅·주석만 고쳤다면 2번만 돌리면 됩니다."}}
EOF
fi

exit 0
