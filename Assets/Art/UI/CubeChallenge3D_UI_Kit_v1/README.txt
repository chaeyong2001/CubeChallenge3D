CubeChallenge3D UI Kit v1
=========================

목표:
- 아까 만든 레퍼런스 UI를 Unity에 최대한 가깝게 구현하기 위한 1차 디자인 자산 패키지입니다.
- 이 단계의 이미지는 '최종 Unity 조립용 기준 이미지/자산 시트'로 사용합니다.
- 아직 각 버튼이 투명 PNG로 완전히 분리된 상태는 아닙니다. 다음 단계에서 필요한 부품을 개별 crop/분리하거나, 같은 스타일로 개별 PNG를 추가 생성합니다.

폴더 구성:
00_reference/
- 메인 메뉴 전체 레퍼런스 앵커 이미지입니다.
- Codex가 전체 분위기/비율/색감 기준으로 참고해야 합니다.

01_hud/
- 상단 하트/코인/보석/Shop HUD 자산 시트입니다.
- Unity에서는 여기서 top bar, heart card, coin card, gem card, shop button 스타일을 기준으로 prefab을 만듭니다.

02_main_menu/
- 메인 메뉴 버튼 7종 자산 시트입니다.
- Stages / Ranking Challenge / Solver & Learn / Shop / Rewards / Records / Settings 버튼 스타일 기준입니다.

03_gameplay_buttons/
- View / Solve / +Move / Hint / Undo / Stage List / Retry / Main Menu / Ranking / Scramble / Start 버튼 스타일 기준입니다.

04_icons/
- 하트, 코인, 보석, 장바구니, 스테이지, 트로피, 전구, 선물, 기록, 설정, +, 화살표 등 아이콘 기준입니다.

05_sample_screens/
- Stage Play / Daily Challenge 샘플 화면입니다.
- Codex가 실제 씬 배치 참고용으로 사용합니다.

권장 사용 방식:
1. 이 zip을 Codex 프로젝트 설명과 함께 첨부합니다.
2. Codex에는 '이미지를 그대로 배경으로 쓰지 말고, UI 구성 기준/자산 시트로 사용하라'고 지시합니다.
3. 먼저 MainMenu만 적용합니다.
4. 만족하면 Stage/Daily로 확장합니다.
5. 더 높은 완성도를 원하면 다음 단계에서 각 시트에서 개별 부품 PNG를 분리하거나, 투명 PNG 부품을 별도로 생성합니다.

주의:
- 이 패키지는 v1 스타일 기준입니다.
- 바로 Unity에 넣어 완성되는 최종 sprite pack이라기보다, Codex 구현 기준 + 추가 분리 작업용입니다.
