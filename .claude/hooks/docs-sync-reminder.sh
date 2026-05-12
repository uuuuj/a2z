#!/usr/bin/env bash
# PostToolUse hook — docs 동기화 리마인더 (CLAUDE.md R1)
#
# 목적: A2Z/Form1.*.cs 파일이 Edit/Write 될 때마다 Claude에게
#       docs/기능/{카테고리}/{기능}.md 동기화를 상기시킨다.
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
{"hookSpecificOutput":{"hookEventName":"PostToolUse","additionalContext":"[docs-sync-reminder] 방금 A2Z/Form1.*.cs 파일을 수정했습니다. 핸들러 흐름이 변경된 경우 CLAUDE.md R1에 따라 다음을 갱신하세요:\n1) docs/기능/{카테고리}/{기능}.md (해당 핸들러 흐름 문서)\n2) docs/code-reference/form1-{category}.md 앵커 라인 번호 (크게 변동됐을 때만)\n3) 두 문서 모두 last_updated 프론트매터 + 변경 이력 표\n단순 리팩토링·포매팅·로깅만 추가했으면 생략 가능합니다."}}
EOF
fi

exit 0
