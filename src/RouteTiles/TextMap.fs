module RouteTiles.App.TextMap

type Buttons = {
  play: string
  ranking: string
  setting: string
  timeattack2000: string
  scoreattack180: string
  tutorial: string
  namesetting: string
  backgroundsetting: string
  save: string
  cancel: string
  start: string
  back: string
}

type Descriptions = {
  play: string
  ranking: string
  setting: string
  timeattack2000: string
  scoreattack180: string
  tutorial: string
  namesetting: string
  backgroundsetting: string
  settingsave: string
}

type TextMap = {
  buttons: Buttons
  descriptions: Descriptions
}

let textMapJapanese = {
  buttons = {
    play = "プレイ"
    ranking = "ランキング"
    setting = "設定"
    timeattack2000 = "タイムアタック2000"
    scoreattack180 = "スコアアタック180"
    tutorial = "チュートリアル"
    namesetting = "お名前"
    backgroundsetting = "背景"
    save = "保存する"
    cancel = "キャンセル"
    start = "スタート"
    back = "戻る"
  }

  descriptions = {
    play = "ゲームをプレイします"
    ranking = "オンラインランキングを確認します"
    setting = "色々な設定を行います"
    timeattack2000 = "2000点までの時間を競うモードです"
    scoreattack180 = "180秒以内のスコアを競うモードです"
    tutorial = "ゲームの遊び方を説明します"
    namesetting = "ランキングで使う名前を設定します"
    backgroundsetting = "背景を設定します"
    settingsave = "設定を保存します"
  }
}
