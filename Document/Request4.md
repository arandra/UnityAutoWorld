# 도메인 데이터 스펙 초안

## 1. 자원(Resource)
| 이름             | 설명                         | 비고              |
|------------------|------------------------------|-------------------|
| PopulationPoint  | 인구 성장에 사용되는 포인트  | 소비 시 감소      |
| TerritoryPoint   | 영토 확장에 필요한 포인트    | 건물 건설 시 소비 |
| Crop             | 농장에서 생산되는 작물       | 소비/생산 자원    |
| Coin             | 교환 혹은 시장 활동에 쓰임   | 화폐 자원         |

## 2. 직업(Job)
| 이름     | 설명                     | 주요 역할                             |
|----------|--------------------------|----------------------------------------|
| Worker   | 범용 작업자              | 건설, 유지보수, 기본 자원 운반        |
| Farmer   | 농장의 생산 담당         | Crop 생산 작업 수행                   |
| Merchant | 시장 및 거래 담당        | Coin 관련 거래, 자원 교환             |

## 3. 건물(Building)
| 이름       | 영토 비용 | 작업 슬롯 수 | 주요 작업 예시                              | 이벤트 트리거                       |
|------------|-----------|--------------|---------------------------------------------|------------------------------------|
| Farm       | 2         | 2            | 농부가 Crop 생산 (`ProduceCrop`)             | 생산 완료 시 `BuildingCompleted`    |
| Residence  | 1         | 1            | Worker가 PopulationPoint 생성 보조 (`SupportPopulation`) | 인구 성장 조건 충족 시 `PopulationGrowth` |
| Warehouse  | 3         | 1            | Worker가 자원 저장/출고 (`ManageStorage`)   | 용량 초과 시 경고 이벤트             |
| Market     | 2         | 1            | Merchant가 Coin/Crop 교환 (`TradeGoods`)     | 거래 성사 시 `JobAssignmentChanged` |

## 4. 작업(Task)
| 이름              | 담당 직업 | 필요 자원                                 | 결과 자원                                   | 소요 시간(ms) |
|-------------------|-----------|---------------------------------------------|----------------------------------------------|---------------|
| ProduceCrop        | Farmer    | TerritoryPoint:1                            | Crop:+3                                      | 5000          |
| SupportPopulation  | Worker    | Crop:2                                      | PopulationPoint:+1                           | 4000          |
| ManageStorage      | Worker    | 없음                                        | 창고 상태 갱신(이벤트로 표현)                | 3000          |
| TradeGoods         | Merchant  | Crop:1, Coin:1                              | Crop:-1, Coin:+2 (이익)                      | 6000          |

## 5. 인구 성장 규칙(PopulationGrowthRule)
| 요구 포인트 | 틱당 증가량 | 부연 설명                           |
|-------------|--------------|------------------------------------|
| 5           | 1            | 5포인트를 모을 때마다 인구 +1       |
| 10          | 2            | 고급 조건 달성 시 추가 성장 가속    |

## 6. 이벤트 흐름
| 이벤트 타입              | 발생 조건 예시                     | 주요 파라미터                          |
|--------------------------|------------------------------------|----------------------------------------|
| TerritoryExpansion       | 건물 건설 완료                     | `Target`: 새 건물, `IntValue`: 비용     |
| PopulationGrowth         | PopulationPoint 누적 조건 충족     | `IntValue`: 증가 인구 수               |
| JobAssignmentChanged     | 작업자가 새로운 작업에 배치될 때   | `Target`: 작업자, `StringValue`: 작업명 |
| BuildingCompleted        | 건물 건설 혹은 업그레이드 완료     | `Target`: 건물, `CustomObject`: 보상    |

## 7. 테스트 시나리오 초안
1. 초기 상태에서 `ManualTickScheduler`로 5회 틱 호출 → 농장 작업이 Crop을 생산하는지 확인.
2. Crop을 소비하여 PopulationPoint 획득 → 누적 5포인트 시 `PopulationGrowth` 이벤트 발생 여부 검사.
3. 창고 저장 용량을 초과시키는 이벤트를 트리거 → 경고 이벤트가 발행되는지 확인.
4. 시장 거래 작업 진행 → Coin 증가와 `JobAssignmentChanged` 이벤트 발생 여부 확인.

## 8. 향후 결정 사항
- 데이터 로딩 경로(JSON, ScriptableObject 등)와 버전 관리 정책.
- 건물/자원 상호 작용을 정의하는 추가 규칙(예: 용량, 유지비, 파손 등).
- 이벤트 파라미터 구조 확대 필요 여부.
