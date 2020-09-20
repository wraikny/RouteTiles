module internal RouteTiles.App.TextMap

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
  continueGame: string
  restartGame: string
  quitGame: string
  changeController: string
  others: string
  keyboard: string
  sendToServer: string
  notSendToServer: string
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
  selectController: string
  continueGame: string
  restartGame: string
  quitGame: string
  changeUsername: string
  namePlaceholder: string
  waitingResponse: string
  sendToServer: string
}

type Modes = {
  gameModeSelect: string
  controllerSelect: string
  ranking: string
  setting: string
  nameSetting: string
  backgroundSetting: string
  pause: string
  gameResult: string
  // waitingResponse: string
}

type TextMap = {
  buttons: Buttons
  descriptions: Descriptions
  modes: Modes
}

let textMapJapanese = {
  buttons = {
    play = "遊ぶ"
    ranking = "ランキング"
    setting = "設定"
    timeattack2000 = "タイム2000"
    scoreattack180 = "スコア180"
    tutorial = "チュートリアル"
    namesetting = "お名前"
    backgroundsetting = "背景"
    save = "保存する"
    cancel = "キャンセル"
    start = "スタート"
    back = "戻る"
    continueGame = "再開"
    restartGame = "はじめから"
    quitGame = "ゲームをやめる"
    changeController = "コントローラ切替"
    others = "キーボード"
    keyboard = "キーボード"
    sendToServer = "送信する"
    notSendToServer = "送信しない"
  }

  descriptions = {
    play = "ゲームを遊びます"
    ranking = "オンラインランキングを確認します"
    setting = "色々な設定を行います"
    timeattack2000 = "2000点までの時間を競うモードです"
    scoreattack180 = "180秒以内のスコアを競うモードです"
    tutorial = "ゲームの遊び方を説明します"
    namesetting = "ランキングで使う名前を設定します"
    backgroundsetting = "背景を設定します"
    settingsave = "設定を保存します"
    selectController = "使いたいコントローラを選びます"
    continueGame = "ゲームを再開します"
    restartGame = "ゲームを始めからやりなおします"
    quitGame = "ゲームをやめてタイトルに戻ります"
    changeUsername = "ユーザ名を設定します"
    namePlaceholder = "username"
    waitingResponse = "しばらくお待ち下さい..."
    sendToServer = "オンラインランキングに結果を送信しますか？"
  }

  modes = {
    gameModeSelect = "ゲームモード選択"
    controllerSelect = "コントローラー選択"
    ranking = "ランキング"
    setting = "設定"
    nameSetting = "お名前設定"
    backgroundSetting = "背景設定"
    pause = "一時停止"
    gameResult = "リザルト"
    // waitingResponse = "通信待機中..."
    // error = "エラー"
  }
}
