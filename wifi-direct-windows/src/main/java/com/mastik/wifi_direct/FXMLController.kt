package com.mastik.wifi_direct

import javafx.fxml.FXML
import javafx.fxml.Initializable
import javafx.scene.control.Label
import javafx.scene.control.ScrollPane
import java.net.URL
import java.util.ResourceBundle


class FXMLController : Initializable {

    @FXML
    private var label: Label? = null

    @FXML
    private var devices: ScrollPane? = null

    override fun initialize(url: URL, rb: ResourceBundle?) {
        val javaVersion = System.getProperty("java.version")
        val javafxVersion = System.getProperty("javafx.version")
        label!!.text = "Hello, JavaFX $javafxVersion\nRunning on Java $javaVersion."
    }
}