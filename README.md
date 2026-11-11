# Chicken Fly!2

닭이 날갯짓으로 날아 땅 → 숲 → 구름 → 우주까지 계속 위로 올라가는 3D 비행 게임(시즌1 「Chicken Fly」의 시즌2).
Unity 6000.6.8f1 · URP · Cinemachine · ProBuilder · hera-agent-unity(에디터 조작용, Packages에 임베드)

## 처음 열었을 때
1. 메뉴 **Chicken Fly ▸ Project Setup (tags · gravity · input)** → 에디터 재시작
2. **Chicken Fly ▸ Build Chicken Model**
3. **Chicken Fly ▸ Build World (10 Stages)** → `Assets/Scenes/ChickenFly_World.unity`를 열고 Play

## 조작
W 전진 · S 브레이크/후진 · 마우스 방향 · Space 날갯짓

## 폴더
- `Assets/Scripts`: 게임 스크립트(`Gimmicks/` 자연물 기믹, `Editor/` 생성기, `Legacy/` 시즌1 수업 스크립트)
- `Assets/Settings/ChickenFly`: 생성기 설정(StageBuildSettings), 구간별 후처리
- `Assets/Resources/FX`: 이펙트 설정 · 파티클 재질
- `Assets/Art`: 생성된 머티리얼 · 텍스처 · 메시
- `Docs/작업기록.md`: 무엇을 만들고 어떻게 고쳐 왔는지
