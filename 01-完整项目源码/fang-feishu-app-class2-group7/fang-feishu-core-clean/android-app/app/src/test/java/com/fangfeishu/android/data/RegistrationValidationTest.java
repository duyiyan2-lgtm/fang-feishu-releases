package com.fangfeishu.android.data;

import org.junit.Test;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertNull;

public final class RegistrationValidationTest {
    @Test
    public void twoCharacterChineseUsername_ShouldPass() {
        assertNull(RegistrationValidationKt.registrationValidationMessage(
            new RegisterRequest("火山", "123456", "火山", null, null, "Android")
        ));
    }

    @Test
    public void shortPassword_ShouldReturnChineseMessage() {
        assertEquals(
            "密码至少需要6个字符",
            RegistrationValidationKt.registrationValidationMessage(
                new RegisterRequest("火山", "1234", "火山", null, null, "Android")
            )
        );
    }
}
